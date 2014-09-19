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

namespace Amqp
{
    using System;
    using Amqp.Types;

    static class AmqpBitConverter
    {
        public static byte[] GetBytes(uint data)
        {
            byte[] bytes = new byte[FixedWidth.UInt];
            WriteInt(bytes, 0, (int)data);
            return bytes;
        }

        public static sbyte ReadByte(ByteBuffer buffer)
        {
            buffer.Validate(false, FixedWidth.UByte);
            sbyte data = (sbyte)buffer.Buffer[buffer.Offset];
            buffer.Complete(FixedWidth.UByte);
            return data;
        }

        public static byte ReadUByte(ByteBuffer buffer)
        {
            return (byte)ReadByte(buffer);
        }

        public static short ReadShort(ByteBuffer buffer)
        {
            buffer.Validate(false, FixedWidth.Short);
            short data = (short)((buffer.Buffer[buffer.Offset] << 8) | buffer.Buffer[buffer.Offset + 1]);
            buffer.Complete(FixedWidth.Short);
            return data;
        }

        public static ushort ReadUShort(ByteBuffer buffer)
        {
            return (ushort)ReadShort(buffer);
        }

        public static int ReadInt(ByteBuffer buffer)
        {
            buffer.Validate(false, FixedWidth.Int);
            int data = ReadInt(buffer.Buffer, buffer.Offset);
            buffer.Complete(FixedWidth.Int);
            return data;
        }

        public static int ReadInt(byte[] buffer, int offset)
        {
            return (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];
        }

        public static uint ReadUInt(ByteBuffer buffer)
        {
            return (uint)ReadInt(buffer);
        }

        public static long ReadLong(ByteBuffer buffer)
        {
            buffer.Validate(false, FixedWidth.Long);
            long high = ReadInt(buffer.Buffer, buffer.Offset);
            long low = (uint) ReadInt(buffer.Buffer, buffer.Offset + 4);
            long data = (high << 32) | low;
            buffer.Complete(FixedWidth.Long);
            return data;
        }

        public static ulong ReadULong(ByteBuffer buffer)
        {
            return (ulong)ReadLong(buffer);
        }

        public static float ReadFloat(ByteBuffer buffer)
        {
            return Fx.ReadFloat(buffer);
        }

        public static double ReadDouble(ByteBuffer buffer)
        {
            return Fx.ReadDouble(buffer);
        }

        public static Guid ReadUuid(ByteBuffer buffer)
        {
            buffer.Validate(false, FixedWidth.Uuid);
            byte[] d = new byte[FixedWidth.Uuid];
            if (Fx.IsLittleEndian)
            {
                int pos = buffer.Offset;
                d[3] = buffer.Buffer[pos++];
                d[2] = buffer.Buffer[pos++];
                d[1] = buffer.Buffer[pos++];
                d[0] = buffer.Buffer[pos++];

                d[5] = buffer.Buffer[pos++];
                d[4] = buffer.Buffer[pos++];

                d[7] = buffer.Buffer[pos++];
                d[6] = buffer.Buffer[pos++];

                Array.Copy(buffer.Buffer, pos, d, 8, 8);
            }
            else
            {
                Array.Copy(buffer.Buffer, buffer.Offset, d, 0, FixedWidth.Uuid);
            }

            buffer.Complete(FixedWidth.Uuid);
            return new Guid(d);
        }

        public static void ReadBytes(ByteBuffer buffer, byte[] data, int offset, int count)
        {
            buffer.Validate(false, count);
            Array.Copy(buffer.Buffer, buffer.Offset, data, offset, count);
            buffer.Complete(count);
        }

        public static void WriteByte(ByteBuffer buffer, sbyte data)
        {
            buffer.Validate(true, FixedWidth.Byte);
            buffer.Buffer[buffer.WritePos] = (byte)data;
            buffer.Append(FixedWidth.Byte);
        }

        public static void WriteUByte(ByteBuffer buffer, byte data)
        {
            WriteByte(buffer, (sbyte)data);
        }

        public static void WriteShort(ByteBuffer buffer, short data)
        {
            buffer.Validate(true, FixedWidth.Short);
            if (Fx.IsLittleEndian)
            {
                buffer.Buffer[buffer.WritePos] = (byte)((data & 0xFF00) >> 8);
                buffer.Buffer[buffer.WritePos + 1] = (byte)(data & 0xFF);
            }
            else
            {
                Fx.InsertValueIntoArray(buffer.Buffer, buffer.WritePos, FixedWidth.Short, (uint)data);
            }

            buffer.Append(FixedWidth.Short);
        }

        public static void WriteUShort(ByteBuffer buffer, ushort data)
        {
            WriteShort(buffer, (short)data);
        }

        public static void WriteInt(ByteBuffer buffer, int data)
        {
            buffer.Validate(true, FixedWidth.Int);
            WriteInt(buffer.Buffer, buffer.WritePos, data);
            buffer.Append(FixedWidth.Int);
        }

        public static void WriteInt(byte[] buffer, int offset, int data)
        {
            if (Fx.IsLittleEndian)
            {
                buffer[offset] = (byte)((data & 0xFF000000) >> 24);
                buffer[offset + 1] = (byte)((data & 0xFF0000) >> 16);
                buffer[offset + 2] = (byte)((data & 0xFF00) >> 8);
                buffer[offset + 3] = (byte)(data & 0xFF);
            }
            else
            {
                Fx.InsertValueIntoArray(buffer, offset, FixedWidth.Int, (uint)data);
            }
        }

        public static void WriteUInt(ByteBuffer buffer, uint data)
        {
            WriteInt(buffer, (int)data);
        }

        public static void WriteLong(ByteBuffer buffer, long data)
        {
            if (Fx.IsLittleEndian)
            {
                WriteInt(buffer, (int)(data >> 32));
                WriteInt(buffer, (int)data);
            }
            else
            {
                Fx.InsertValueIntoArray(buffer.Buffer, buffer.WritePos, FixedWidth.Int, (uint)(data >> 32));
                Fx.InsertValueIntoArray(buffer.Buffer, buffer.WritePos, FixedWidth.Int, (uint)data);
                buffer.Append(FixedWidth.Long);
            }
        }

        public static void WriteULong(ByteBuffer buffer, ulong data)
        {
            WriteLong(buffer, (long)data);
        }

        public static void WriteFloat(ByteBuffer buffer, float data)
        {
            Fx.WriteFloat(buffer, data);
        }

        public static void WriteDouble(ByteBuffer buffer, double data)
        {
            Fx.WriteDouble(buffer, data);
        }

        public static void WriteUuid(ByteBuffer buffer, Guid data)
        {
            buffer.Validate(true, FixedWidth.Uuid);
            byte[] p = data.ToByteArray();
            int pos = buffer.WritePos;
            
            buffer.Buffer[pos++] = p[3];
            buffer.Buffer[pos++] = p[2];
            buffer.Buffer[pos++] = p[1];
            buffer.Buffer[pos++] = p[0];

            buffer.Buffer[pos++] = p[5];
            buffer.Buffer[pos++] = p[4];

            buffer.Buffer[pos++] = p[7];
            buffer.Buffer[pos++] = p[6];

            Array.Copy(p, 8, buffer.Buffer, pos, 8);

            buffer.Append(FixedWidth.Uuid);
        }

        public static void WriteBytes(ByteBuffer buffer, byte[] data, int offset, int count)
        {
            buffer.Validate(true, count);
            Array.Copy(data, offset, buffer.Buffer, buffer.WritePos, count);
            buffer.Append(count);
        }
    }
}
