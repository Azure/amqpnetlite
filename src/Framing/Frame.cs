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

    enum FrameType : byte
    {
        Amqp = 0,
        Sasl = 1
    }

    static class Frame
    {
        const byte DOF = 2;

        public static ByteBuffer GetBuffer(FrameType type, ushort channel, DescribedList command, int initialSize, int payloadSize)
        {
            ByteBuffer buffer = new ByteBuffer(initialSize, true);
            AmqpBitConverter.WriteUInt(buffer, 0u);
            AmqpBitConverter.WriteUByte(buffer, DOF);
            AmqpBitConverter.WriteUByte(buffer, (byte)type);
            AmqpBitConverter.WriteUShort(buffer, channel);
            if (command != null) command.Encode(buffer);
            AmqpBitConverter.WriteInt(buffer.Buffer, 0, buffer.Length + payloadSize);
            return buffer;
        }

        public static void GetFrame(ByteBuffer buffer, out ushort channel, out DescribedList command)
        {
            AmqpBitConverter.ReadUInt(buffer);
            AmqpBitConverter.ReadUByte(buffer);
            AmqpBitConverter.ReadUByte(buffer);
            channel = AmqpBitConverter.ReadUShort(buffer);
            command = (DescribedList)Encoder.ReadObject(buffer);
        }
    }
}