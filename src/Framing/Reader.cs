//  ------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation
//  All rights reserved. 
//  
//  Licensed under the Apache License, Version 2.0 (the ""License""); you may not use this 
//  file except in compliance with the License. You may obtain a copy of the License at 
//  http://www.apache.org/licenses/LICENSE-2.0  
//  
//  THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
//  EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
//  CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR 
//  NON-INFRINGEMENT. 
// 
//  See the Apache Version 2.0 License for specific language governing permissions and 
//  limitations under the License.
//  ------------------------------------------------------------------------------------

namespace Amqp.Framing
{
    using System;
    using Amqp.Types;

    static class Reader
    {
        public static ProtocolHeader ReadHeader(ITransport transport)
        {
            byte[] smallBuffer = new byte[8];
            ReadBuffer(transport, smallBuffer, 0, 8);
            return ProtocolHeader.Create(smallBuffer, 0);
        }

        public static ByteBuffer ReadFrameBuffer(ITransport transport, byte[] sizeBuffer, uint maxFrameSize)
        {
            ReadBuffer(transport, sizeBuffer, 0, FixedWidth.UInt);
            int size = AmqpBitConverter.ReadInt(sizeBuffer, 0);
            if ((uint)size > maxFrameSize)
            {
                throw new AmqpException(ErrorCode.InvalidField,
                    Fx.Format(SRAmqp.InvalidFrameSize, size, maxFrameSize));
            }

            ByteBuffer frameBuffer = new ByteBuffer(size, true);
            AmqpBitConverter.WriteInt(frameBuffer, size);
            ReadBuffer(transport, frameBuffer.Buffer, frameBuffer.Length, frameBuffer.Size);
            frameBuffer.Append(frameBuffer.Size);
            return frameBuffer;
        }

        static void ReadBuffer(ITransport transport, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int bytes = transport.Receive(buffer, offset, count);
                if (bytes == 0)
                {
                    throw new OperationCanceledException(Fx.Format(SRAmqp.TransportClosed, transport.GetType().Name));
                }

                offset += bytes;
                count -= bytes;
            }
        }
    }
}