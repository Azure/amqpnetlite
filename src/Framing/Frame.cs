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
    using Amqp.Types;

    enum FrameType : byte
    {
        Amqp = 0,
        Sasl = 1
    }

    static class Frame
    {
        public const int CmdBufferSize = 128;
        const byte DOF = 2;

        public static void Decode(ByteBuffer buffer, out ushort channel, out DescribedList command)
        {
            AmqpBitConverter.ReadUInt(buffer);
            AmqpBitConverter.ReadUByte(buffer);
            AmqpBitConverter.ReadUByte(buffer);
            channel = AmqpBitConverter.ReadUShort(buffer);
            if (buffer.Length > 0)
            {
                command = (DescribedList)Codec.Decode(buffer);
            }
            else
            {
                command = null;
            }
        }

        public static void Encode(ByteBuffer buffer, FrameType type, ushort channel, DescribedList command)
        {
            buffer.Append(FixedWidth.UInt);
            AmqpBitConverter.WriteUByte(buffer, DOF);
            AmqpBitConverter.WriteUByte(buffer, (byte)type);
            AmqpBitConverter.WriteUShort(buffer, channel);
            Codec.Encode(command, buffer);
            AmqpBitConverter.WriteInt(buffer.Buffer, buffer.Offset, buffer.Length);
        }
    }
}