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
#if MF_FRAMEWORK_VERSION_V4_2
    using Microsoft.SPOT;
    using Microsoft.SPOT.Hardware;
#endif

    public static class AmqpBitConverter
    {
#if MF_FRAMEWORK_VERSION_V4_2
        public static readonly bool IsLittleEndian = Utility.ExtractValueFromArray(new byte[] { 0x01, 0x02 }, 0, 2) == 0x0201u;
#else
        public static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;
#endif

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
            long low = (uint)ReadInt(buffer.Buffer, buffer.Offset + 4);
            long data = (high << 32) | low;
            buffer.Complete(FixedWidth.Long);
            return data;
        }

        public static ulong ReadULong(ByteBuffer buffer)
        {
            return (ulong)ReadLong(buffer);
        }

        public static unsafe float ReadFloat(ByteBuffer buffer)
        {
            int data = ReadInt(buffer);
            return *((float*)&data);
        }

        public static unsafe double ReadDouble(ByteBuffer buffer)
        {
            long data = ReadLong(buffer);
            return *((double*)&data);
        }

        public static Guid ReadUuid(ByteBuffer buffer)
        {
            buffer.Validate(false, FixedWidth.Uuid);
            byte[] d = new byte[FixedWidth.Uuid];
            if (AmqpBitConverter.IsLittleEndian)
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
            buffer.Buffer[buffer.WritePos] = (byte)(data >> 8);
            buffer.Buffer[buffer.WritePos + 1] = (byte)data;
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
            buffer[offset] = (byte)(data >> 24);
            buffer[offset + 1] = (byte)(data >> 16);
            buffer[offset + 2] = (byte)(data >> 8);
            buffer[offset + 3] = (byte)data;
        }

        public static void WriteUInt(ByteBuffer buffer, uint data)
        {
            WriteInt(buffer, (int)data);
        }

        public static void WriteLong(ByteBuffer buffer, long data)
        {
            WriteInt(buffer, (int)(data >> 32));
            WriteInt(buffer, (int)data);
        }

        public static void WriteULong(ByteBuffer buffer, ulong data)
        {
            WriteLong(buffer, (long)data);
        }

        public static unsafe void WriteFloat(ByteBuffer buffer, float data)
        {
            int n = *((int*)&data);
            WriteInt(buffer, n);
        }

        public static unsafe void WriteDouble(ByteBuffer buffer, double data)
        {
            long n = *((long*)&data);
            WriteLong(buffer, n);
        }

        public static void WriteUuid(ByteBuffer buffer, Guid data)
        {
            buffer.Validate(true, FixedWidth.Uuid);
            byte[] p = data.ToByteArray();
            int pos = buffer.WritePos;

            if (AmqpBitConverter.IsLittleEndian)
            {
                buffer.Buffer[pos++] = p[3];
                buffer.Buffer[pos++] = p[2];
                buffer.Buffer[pos++] = p[1];
                buffer.Buffer[pos++] = p[0];

                buffer.Buffer[pos++] = p[5];
                buffer.Buffer[pos++] = p[4];

                buffer.Buffer[pos++] = p[7];
                buffer.Buffer[pos++] = p[6];

                Array.Copy(p, 8, buffer.Buffer, pos, 8);
            }
            else
            {
                Array.Copy(p, buffer.Buffer, FixedWidth.Uuid);
            }

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
