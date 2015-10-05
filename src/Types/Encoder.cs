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

namespace Amqp.Types
{
    using System;
    using System.Collections;
    using System.Text;
    using System.Globalization;

    /// <summary>
    /// The delegate to create a decribed object.
    /// </summary>
    /// <returns></returns>
    public delegate Described CreateDescribed();

#if SMALL_MEMORY
    /// <summary>
    /// Encodes or decodes AMQP types.
    /// </summary>
    public static class Encoder
    {

#if NETMF
        // NETMF DateTime ticks origin is 1601/1/1
        const long epochTicks = 116444736000000000; // 1970-1-1 00:00:00 UTC
#else
        const long epochTicks = 621355968000000000; // 1970-1-1 00:00:00 UTC
#endif
        const long ticksPerMillisecond = 10000;


        static Encoder()
        {
        }

        /// <summary>
        /// Converts a DateTime value to AMQP timestamp (milliseconds from Unix epoch)
        /// </summary>
        /// <param name="dateTime">The DateTime value to convert.</param>
        /// <returns></returns>
        public static long DateTimeToTimestamp(DateTime dateTime)
        {
            return (long)((dateTime.ToUniversalTime().Ticks - epochTicks) / ticksPerMillisecond);
        }

        /// <summary>
        /// Converts an AMQP timestamp ((milliseconds from Unix epoch)) to a DateTime.
        /// </summary>
        /// <param name="timestamp">The AMQP timestamp to convert.</param>
        /// <returns></returns>
        public static DateTime TimestampToDateTime(long timestamp)
        {
            return new DateTime(epochTicks + timestamp * ticksPerMillisecond, DateTimeKind.Utc);
        }

        internal static byte ReadFormatCode(ref ByteBuffer buffer)
        {
            return AmqpBitConverter.ReadUByte(ref buffer);
        }

        /// <summary>
        /// Writes an AMQP object to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The AMQP value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteObject(ref ByteBuffer buffer, object value, bool smallEncoding = true)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Null);
            }
            else
            {
                var objectType = value.GetType();

                if (objectType == null)
                {
                    AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Null);
                }
                // 1: boolean
                else if (objectType == typeof(bool))
                {
                    WriteBoolean(ref buffer, (bool)value, smallEncoding);
                }
                // 2: ubyte
                else if (objectType == typeof(byte))
                {
                    WriteUByte(ref buffer, (byte)value);
                }
                // 3: ushort
                else if (objectType == typeof(ushort))
                {
                    WriteUShort(ref buffer, (ushort)value);
                }
                // 4: uint
                else if (objectType == typeof(uint))
                {
                    WriteUInt(ref buffer, (uint)value, smallEncoding);
                }
                // 5: ulong
                else if (objectType == typeof(ulong))
                {
                    WriteULong(ref buffer, (ulong)value, smallEncoding);
                }
                // 6: byte
                else if (objectType == typeof(sbyte))
                {
                    WriteByte(ref buffer, (sbyte)value);
                }
                // 7: short
                else if (objectType == typeof(short))
                {
                    WriteShort(ref buffer, (short)value);
                }
                // 8: int
                else if (objectType == typeof(int))
                {
                    WriteInt(ref buffer, (int)value, smallEncoding);
                }
                // 9: long
                else if (objectType == typeof(long))
                {
                    WriteLong(ref buffer, (long)value, smallEncoding);
                }
                // 10: float
                else if (objectType == typeof(float))
                {
                    WriteFloat(ref buffer, (float)value);
                }
                // 11: double
                else if (objectType == typeof(double))
                {
                    WriteDouble(ref buffer, (double)value);
                }
                // 12: char
                else if (objectType == typeof(char))
                {
                    WriteChar(ref buffer, (char)value);
                }
                // 13: timestamp
                else if (objectType == typeof(DateTime))
                {
                    WriteTimestamp(ref buffer, (DateTime)value);
                }
                // 14: uuid
                else if (objectType == typeof(Guid))
                {
                    WriteUuid(ref buffer, (Guid)value);
                }
                // 15: binary
                else if (objectType == typeof(byte[]))
                {
                    WriteBinary(ref buffer, (byte[])value, smallEncoding);
                }
                // 16: string
                else if (objectType == typeof(string))
                {
                    WriteString(ref buffer, (string)value, smallEncoding);
                }
                // 17: symbol
                else if (objectType == typeof(Symbol))
                {
                    WriteSymbol(ref buffer, (Symbol)value, smallEncoding);
                }
                // 18: list
                else if (objectType == typeof(List))
                {
                    WriteList(ref buffer, (IList)value, smallEncoding);
                }
                // 19: map
                else if (objectType == typeof(Map))
                {
                    WriteMap(ref buffer, (Map)value, smallEncoding);
                }
                // 20: array
                else if (objectType == typeof(Array))
                {
                    WriteArray(ref buffer, (Array)value);
                }
                // 21: invalid
                else
                {
                    // try if this is a described 
                    if (value is Described)
                    {
                        ((Described)value).Encode(ref buffer);
                    }
                    else
                    {
                        throw new AmqpException(ErrorCode.EncodingTypeNotSupported, value.GetType().ToString());
                    }
                }
            }
        }

        public static void WriteBoolean(ref ByteBuffer buffer, bool value, bool smallEncoding)
        {
            if (smallEncoding)
            {
                AmqpBitConverter.WriteUByte(ref buffer, value ? FormatCode.BooleanTrue : FormatCode.BooleanFalse);
            }
            else
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Boolean);
                AmqpBitConverter.WriteUByte(ref buffer, (byte)(value ? 1 : 0));
            }
        }

        /// <summary>
        /// Writes an unsigned byte value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The unsigned byte value.</param>
        public static void WriteUByte(ref ByteBuffer buffer, byte value)
        {
            AmqpBitConverter.WriteUByte(ref buffer, FormatCode.UByte);
            AmqpBitConverter.WriteUByte(ref buffer, value);
        }

        /// <summary>
        /// Writes an unsigned 16-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The unsigned 16-bit integer value.</param>
        public static void WriteUShort(ref ByteBuffer buffer, ushort value)
        {
            AmqpBitConverter.WriteUByte(ref buffer, FormatCode.UShort);
            AmqpBitConverter.WriteUShort(ref buffer, value);
        }

        /// <summary>
        /// Writes an unsigned 32-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The unsigned 32-bit integer value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteUInt(ref ByteBuffer buffer, uint value, bool smallEncoding)
        {
            if (!smallEncoding || value > byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.UInt);
                AmqpBitConverter.WriteUInt(buffer, value);
            }
            else if (value == 0)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.UInt0);
            }
            else if (value <= byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.SmallUInt);
                AmqpBitConverter.WriteUByte(ref buffer, (byte)value);
            }
        }

        /// <summary>
        /// Writes an unsigned 64-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The unsigned 64-bit integer value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteULong(ref ByteBuffer buffer, ulong value, bool smallEncoding)
        {
            if (!smallEncoding || value > byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.ULong);
                AmqpBitConverter.WriteULong(ref buffer, value);
            }
            else if (value == 0)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.ULong0);
            }
            else if (value <= byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.SmallULong);
                AmqpBitConverter.WriteUByte(ref buffer, (byte)value);
            }
        }

        /// <summary>
        /// Writes a signed byte value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The signed byte value.</param>
        public static void WriteByte(ref ByteBuffer buffer, sbyte value)
        {
            AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Byte);
            AmqpBitConverter.WriteByte(ref buffer, value);
        }

        /// <summary>
        /// Writes a signed 16-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The signed 16-bit integer value.</param>
        public static void WriteShort(ref ByteBuffer buffer, short value)
        {
            AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Short);
            AmqpBitConverter.WriteShort(ref buffer, value);
        }

        /// <summary>
        /// Writes a signed 32-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The signed 32-bit integer value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteInt(ref ByteBuffer buffer, int value, bool smallEncoding)
        {
            if (smallEncoding && value >= sbyte.MinValue && value <= sbyte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.SmallInt);
                AmqpBitConverter.WriteByte(ref buffer, (sbyte)value);
            }
            else
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Int);
                AmqpBitConverter.WriteInt(buffer, value);
            }
        }

        /// <summary>
        /// Writes a signed 64-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The signed 64-bit integer value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteLong(ref ByteBuffer buffer, long value, bool smallEncoding)
        {
            if (smallEncoding && value >= sbyte.MinValue && value <= sbyte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.SmallLong);
                AmqpBitConverter.WriteByte(ref buffer, (sbyte)value);
            }
            else
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Long);
                AmqpBitConverter.WriteLong(ref buffer, value);
            }
        }

        /// <summary>
        /// Writes a char value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The char value.</param>
        public static void WriteChar(ref ByteBuffer buffer, char value)
        {
            AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Char);
            AmqpBitConverter.WriteInt(buffer, value);   // TODO: utf32
        }

        /// <summary>
        /// Writes a 32-bit floating-point value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The 32-bit floating-point value.</param>
        public static void WriteFloat(ref ByteBuffer buffer, float value)
        {
            AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Float);
            AmqpBitConverter.WriteFloat(buffer, value);
        }

        /// <summary>
        /// Writes a 64-bit floating-point value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The 64-bit floating-point value.</param>
        public static void WriteDouble(ref ByteBuffer buffer, double value)
        {
            AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Double);
            AmqpBitConverter.WriteDouble(ref buffer, value);
        }

        /// <summary>
        /// Writes a timestamp value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The timestamp value which is the milliseconds since UNIX epoch.</param>
        public static void WriteTimestamp(ref ByteBuffer buffer, DateTime value)
        {
            AmqpBitConverter.WriteUByte(ref buffer, FormatCode.TimeStamp);
            AmqpBitConverter.WriteLong(ref buffer, DateTimeToTimestamp(value));
        }

        /// <summary>
        /// Writes a uuid value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The uuid value.</param>
        public static void WriteUuid(ref ByteBuffer buffer, Guid value)
        {
            AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Uuid);
            AmqpBitConverter.WriteUuid(ref buffer, value);
        }

        /// <summary>
        /// Writes a binary value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The binary value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteBinary(ref ByteBuffer buffer, byte[] value, bool smallEncoding)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Null);
            }
            else if (smallEncoding && value.Length <= byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Binary8);
                AmqpBitConverter.WriteUByte(ref buffer, (byte)value.Length);
                AmqpBitConverter.WriteBytes(buffer, value, 0, value.Length);
            }
            else
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Binary32);
                AmqpBitConverter.WriteUInt(buffer, (uint)value.Length);
                AmqpBitConverter.WriteBytes(buffer, value, 0, value.Length);
            }
        }

        /// <summary>
        /// Writes a string value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The string value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteString(ref ByteBuffer buffer, string value, bool smallEncoding)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Null);
            }
            else
            {
                byte[] data = Encoding.UTF8.GetBytes(value);
                if (smallEncoding && data.Length <= byte.MaxValue)
                {
                    AmqpBitConverter.WriteUByte(ref buffer, FormatCode.String8Utf8);
                    AmqpBitConverter.WriteUByte(ref buffer, (byte)data.Length);
                    AmqpBitConverter.WriteBytes(buffer, data, 0, data.Length);
                }
                else
                {
                    AmqpBitConverter.WriteUByte(ref buffer, FormatCode.String32Utf8);
                    AmqpBitConverter.WriteUInt(buffer, (uint)data.Length);
                    AmqpBitConverter.WriteBytes(buffer, data, 0, data.Length);
                }
            }
        }

        /// <summary>
        /// Writes a symbol value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The symbol value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteSymbol(ref ByteBuffer buffer, Symbol value, bool smallEncoding)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Null);
            }
            else
            {
                byte[] data = Encoding.UTF8.GetBytes(value);
                if (smallEncoding && data.Length <= byte.MaxValue)
                {
                    AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Symbol8);
                    AmqpBitConverter.WriteUByte(ref buffer, (byte)data.Length);
                    AmqpBitConverter.WriteBytes(buffer, data, 0, data.Length);
                }
                else
                {
                    AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Symbol32);
                    AmqpBitConverter.WriteUInt(buffer, (uint)data.Length);
                    AmqpBitConverter.WriteBytes(buffer, data, 0, data.Length);
                }
            }
        }

        /// <summary>
        /// Writes a list value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The list value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteList(ref ByteBuffer buffer, IList value, bool smallEncoding)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Null);
            }
            else
            {
                // trim tailing nulls
                int last = value.Count - 1;
                while (last >= 0 && value[last] == null)
                {
                    --last;
                }

                if (last < 0 && smallEncoding)
                {
                    AmqpBitConverter.WriteUByte(ref buffer, FormatCode.List0);
                }
                else
                {
                    int pos = buffer.WritePos;
                    AmqpBitConverter.WriteUByte(ref buffer, 0);
                    AmqpBitConverter.WriteUInt(buffer, 0);
                    AmqpBitConverter.WriteUInt(buffer, 0);

                    for (int i = 0; i <= last; ++i)
                    {
                        Encoder.WriteObject(ref buffer, value[i], smallEncoding);
                    }

                    int size = buffer.WritePos - pos - 9;
                    int count = last + 1;

                    if (smallEncoding && size < byte.MaxValue && count <= byte.MaxValue)
                    {
                        buffer.Buffer[pos] = FormatCode.List8;
                        buffer.Buffer[pos + 1] = (byte)(size + 1);
                        buffer.Buffer[pos + 2] = (byte)count;
                        Array.Copy(buffer.Buffer, pos + 9, buffer.Buffer, pos + 3, size);
                        buffer.Shrink(6);
                    }
                    else
                    {
                        buffer.Buffer[pos] = FormatCode.List32;
                        AmqpBitConverter.WriteInt(buffer.Buffer, pos + 1, size + 4);
                        AmqpBitConverter.WriteInt(buffer.Buffer, pos + 5, count);
                    }
                }
            }
        }

        /// <summary>
        /// Writes an array value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The array value.</param>
        public static void WriteArray(ref ByteBuffer buffer, Array value)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Null);
            }
            else
            {
                int count = value.Length;
                int pos = buffer.WritePos;
                AmqpBitConverter.WriteUByte(ref buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);

                for (int i = 0; i < count; ++i)
                {
                    object item = value.GetValue(i);
                    if (i == 0)
                    {
                        Encoder.WriteObject(ref buffer, item, false);
                    }
                    else
                    {
                        int lastPos = buffer.WritePos - 1;
                        byte lastByte = buffer.Buffer[lastPos];
                        buffer.Shrink(1);
                        Encoder.WriteObject(ref buffer, item, false);
                        buffer.Buffer[lastPos] = lastByte;
                    }
                }

                int size = buffer.WritePos - pos - 9;

                if (size < byte.MaxValue && count <= byte.MaxValue)
                {
                    buffer.Buffer[pos] = FormatCode.Array8;
                    buffer.Buffer[pos + 1] = (byte)(size + 1);
                    buffer.Buffer[pos + 2] = (byte)count;
                    Array.Copy(buffer.Buffer, pos + 9, buffer.Buffer, pos + 3, size);
                    buffer.Shrink(6);
                }
                else
                {
                    buffer.Buffer[pos] = FormatCode.Array32;
                    AmqpBitConverter.WriteInt(buffer.Buffer, pos + 1, size + 4);
                    AmqpBitConverter.WriteInt(buffer.Buffer, pos + 5, count);
                }
            }
        }

        /// <summary>
        /// Writes a map value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The map value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteMap(ref ByteBuffer buffer, Map value, bool smallEncoding)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(ref buffer, FormatCode.Null);
            }
            else
            {
                int pos = buffer.WritePos;
                AmqpBitConverter.WriteUByte(ref buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);

                foreach (var key in value.Keys)
                {
                    Encoder.WriteObject(ref buffer, key);
                    Encoder.WriteObject(ref buffer, value[key]);
                }

                int size = buffer.WritePos - pos - 9;
                int count = value.Count * 2;

                if (smallEncoding && size < byte.MaxValue && count <= byte.MaxValue)
                {
                    buffer.Buffer[pos] = FormatCode.Map8;
                    buffer.Buffer[pos + 1] = (byte)(size + 1);
                    buffer.Buffer[pos + 2] = (byte)count;
                    Array.Copy(buffer.Buffer, pos + 9, buffer.Buffer, pos + 3, size);
                    buffer.Shrink(6);
                }
                else
                {
                    buffer.Buffer[pos] = FormatCode.Map32;
                    AmqpBitConverter.WriteInt(buffer.Buffer, pos + 1, size + 4);
                    AmqpBitConverter.WriteInt(buffer.Buffer, pos + 5, count);
                }
            }
        }

        /// <summary>
        /// Reads an object from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        public static object ReadObject(ref ByteBuffer buffer)
        {
            byte formatCode = Encoder.ReadFormatCode(ref buffer);
            return ReadObject(ref buffer, formatCode);
        }

        /// <summary>
        /// Reads an object from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        /// <returns></returns>
        public static object ReadObject(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Described)
            {
                return ReadDescribed(ref buffer, formatCode);
            }

            return Decode(ref buffer, formatCode);

            throw DecodeException(formatCode, buffer.Offset);
        }

        /// <summary>
        /// Reads a described value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static object ReadDescribed(ref ByteBuffer buffer, byte formatCode)
        {
            Described described;
            object descriptor = Encoder.ReadObject(ref buffer);
            CreateDescribed create = null;

            //if (typeof(descriptor).Equals)
            //if (descriptor.GetType() == typeof(Descriptor))
            //{
            switch ((ulong)descriptor)
            {
                // transport performatives
                // Open = new Descriptor(0x0000000000000010, "");
                case 0x0000000000000010:
                    create = (CreateDescribed)(() => new Amqp.Framing.Open());
                    break;

                // Begin = new Descriptor(0x0000000000000011, "");
                case 0x0000000000000011:
                    create = (CreateDescribed)(() => new Amqp.Framing.Begin());
                    break;

                // Attach = new Descriptor(0x0000000000000012, "");
                case 0x0000000000000012:
                    create = (CreateDescribed)(() => new Amqp.Framing.Attach());
                    break;

                // Flow = new Descriptor(0x0000000000000013, "");
                case 0x0000000000000013:
                    create = (CreateDescribed)(() => new Amqp.Framing.Flow());
                    break;

                // Transfer = new Descriptor(0x0000000000000014, "");
                case 0x0000000000000014:
                    create = (CreateDescribed)(() => new Amqp.Framing.Transfer());
                    break;

                // Dispose = new Descriptor(0x0000000000000015, "");
                case 0x0000000000000015:
                    create = (CreateDescribed)(() => new Amqp.Framing.Dispose());
                    break;

                // Detach = new Descriptor(0x0000000000000016, "");
                case 0x0000000000000016:
                    create = (CreateDescribed)(() => new Amqp.Framing.Detach());
                    break;

                // End = new Descriptor(0x0000000000000017, "");
                case 0x0000000000000017:
                    create = (CreateDescribed)(() => new Amqp.Framing.End());
                    break;

                // Close = new Descriptor(0x0000000000000018, "");
                case 0x0000000000000018:
                    create = (CreateDescribed)(() => new Amqp.Framing.Close());
                    break;


                // Error = new Descriptor(0x000000000000001d, "");
                case 0x000000000000001d:
                    create = (CreateDescribed)(() => new Amqp.Framing.Error());
                    break;


                // outcome
                // Received = new Descriptor(0x0000000000000023, "");
                case 0x0000000000000023:
                    create = (CreateDescribed)(() => new Amqp.Framing.Received());
                    break;

                // Accepted = new Descriptor(0x0000000000000024, "");
                case 0x0000000000000024:
                    create = (CreateDescribed)(() => new Amqp.Framing.Accepted());
                    break;

                // Rejected = new Descriptor(0x0000000000000025, "");
                case 0x0000000000000025:
                    create = (CreateDescribed)(() => new Amqp.Framing.Rejected());
                    break;

                // Released = new Descriptor(0x0000000000000026, "");
                case 0x0000000000000026:
                    create = (CreateDescribed)(() => new Amqp.Framing.Released());
                    break;

                // Modified = new Descriptor(0x0000000000000027, "");
                case 0x0000000000000027:
                    create = (CreateDescribed)(() => new Amqp.Framing.Modified());
                    break;

                // Source =   new Descriptor(0x0000000000000028, "");
                case 0x0000000000000028:
                    create = (CreateDescribed)(() => new Amqp.Framing.Source());
                    break;

                // Target =   new Descriptor(0x0000000000000029, "");
                case 0x0000000000000029:
                    create = (CreateDescribed)(() => new Amqp.Framing.Target());
                    break;

                // sasl
                // SaslMechanisms = new Descriptor(0x0000000000000040, "");
                case 0x0000000000000040:
                    create = (CreateDescribed)(() => new Amqp.Sasl.SaslMechanisms());
                    break;

                // SaslInit = new Descriptor(0x0000000000000041, "");
                case 0x0000000000000041:
                    create = (CreateDescribed)(() => new Amqp.Sasl.SaslInit());
                    break;

                // SaslChallenge = new Descriptor(0x0000000000000042, "");
                case 0x0000000000000042:
                    create = (CreateDescribed)(() => new Amqp.Sasl.SaslChallenge());
                    break;

                // SaslResponse = new Descriptor(0x0000000000000043, "");
                case 0x0000000000000043:
                    create = (CreateDescribed)(() => new Amqp.Sasl.SaslResponse());
                    break;

                // SaslOutcome = new Descriptor(0x0000000000000044, "");
                case 0x0000000000000044:
                    create = (CreateDescribed)(() => new Amqp.Sasl.SaslOutcome());
                    break;

                // message
                // Header = new Descriptor(0x0000000000000070, "");
                case 0x0000000000000070:
                    create = (CreateDescribed)(() => new Amqp.Framing.Header());
                    break;

                // DeliveryAnnotations = new Descriptor(0x0000000000000071, "");
                case 0x0000000000000071:
                    create = (CreateDescribed)(() => new Amqp.Framing.DeliveryAnnotations());
                    break;

                // MessageAnnotations = new Descriptor(0x0000000000000072, "");
                case 0x0000000000000072:
                    create = (CreateDescribed)(() => new Amqp.Framing.MessageAnnotations());
                    break;

                // Properties = new Descriptor(0x0000000000000073, "");
                case 0x0000000000000073:
                    create = (CreateDescribed)(() => new Amqp.Framing.Properties());
                    break;

                // ApplicationProperties = new Descriptor(0x0000000000000074, "");
                case 0x0000000000000074:
                    create = (CreateDescribed)(() => new Amqp.Framing.ApplicationProperties());
                    break;

                // Data = new Descriptor(0x0000000000000075, "");
                case 0x0000000000000075:
                    create = (CreateDescribed)(() => new Amqp.Framing.Data());
                    break;

                // AmqpSequence = new Descriptor(0x0000000000000076, "");
                case 0x0000000000000076:
                    create = (CreateDescribed)(() => new Amqp.Framing.AmqpSequence());
                    break;

                // AmqpValue = new Descriptor(0x0000000000000077, "");
                case 0x0000000000000077:
                    create = (CreateDescribed)(() => new Amqp.Framing.AmqpValue());
                    break;

                // Footer = new Descriptor(0x0000000000000078, "");
                case 0x0000000000000078:
                    create = (CreateDescribed)(() => new Amqp.Framing.Footer());
                    break;
            }
            // }

            if (create == null)
            {
                object value = Encoder.ReadObject(ref buffer);
                described = new DescribedValue(descriptor, value);
            }
            else
            {
                described = create();
                described.DecodeValue(ref buffer);
            }

            return described;
        }

        /// <summary>
        /// Reads a boolean value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static bool ReadBoolean(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.BooleanTrue)
            {
                return true;
            }
            else if (formatCode == FormatCode.BooleanFalse)
            {
                return false;
            }
            else if (formatCode == FormatCode.Boolean)
            {
                byte data = AmqpBitConverter.ReadUByte(ref buffer);
                return data != 0;
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads an unsigned byte value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static byte ReadUByte(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.UByte)
            {
                return AmqpBitConverter.ReadUByte(ref buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads an unsigned 16-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static ushort ReadUShort(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.UShort)
            {
                return AmqpBitConverter.ReadUShort(ref buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads an unsigned 32-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static uint ReadUInt(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.UInt0)
            {
                return 0;
            }
            else if (formatCode == FormatCode.SmallUInt)
            {
                return AmqpBitConverter.ReadUByte(ref buffer);
            }
            else if (formatCode == FormatCode.UInt)
            {
                return AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads an unsigned 64-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static ulong ReadULong(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.ULong0)
            {
                return 0;
            }
            else if (formatCode == FormatCode.SmallULong)
            {
                return AmqpBitConverter.ReadUByte(ref buffer);
            }
            else if (formatCode == FormatCode.ULong)
            {
                return AmqpBitConverter.ReadULong(ref buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a signed byte from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static sbyte ReadByte(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Byte)
            {
                return AmqpBitConverter.ReadByte(ref buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a signed 16-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static short ReadShort(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Short)
            {
                return AmqpBitConverter.ReadShort(ref buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a signed 32-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static int ReadInt(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.SmallInt)
            {
                return AmqpBitConverter.ReadByte(ref buffer);
            }
            else if (formatCode == FormatCode.Int)
            {
                return AmqpBitConverter.ReadInt(ref buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a signed 64-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static long ReadLong(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.SmallLong)
            {
                return AmqpBitConverter.ReadByte(ref buffer);
            }
            else if (formatCode == FormatCode.Long)
            {
                return AmqpBitConverter.ReadLong(ref buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a char value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static char ReadChar(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Char)
            {
                return (char)AmqpBitConverter.ReadInt(ref buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a 32-bit floating-point value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static float ReadFloat(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Float)
            {
                return AmqpBitConverter.ReadFloat(ref buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a 64-bit floating-point value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static double ReadDouble(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Double)
            {
                return AmqpBitConverter.ReadDouble(ref buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a timestamp value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static DateTime ReadTimestamp(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.TimeStamp)
            {
                return TimestampToDateTime(AmqpBitConverter.ReadLong(ref buffer));
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a uuid value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static Guid ReadUuid(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Uuid)
            {
                return AmqpBitConverter.ReadUuid(ref buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a binary value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static byte[] ReadBinary(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            int count;
            if (formatCode == FormatCode.Binary8)
            {
                count = AmqpBitConverter.ReadUByte(ref buffer);
            }
            else if (formatCode == FormatCode.Binary32)
            {
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }

            buffer.Validate(false, count);
            byte[] value = new byte[count];
            Array.Copy(buffer.Buffer, buffer.Offset, value, 0, count);
            buffer.Complete(count);

            return value;
        }

        /// <summary>
        /// Reads a string value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static string ReadString(ref ByteBuffer buffer, byte formatCode)
        {
            return ReadString(ref buffer, formatCode, FormatCode.String8Utf8, FormatCode.String32Utf8, "string");
        }

        /// <summary>
        /// Reads a symbol value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static Symbol ReadSymbol(ref ByteBuffer buffer, byte formatCode)
        {
            return ReadString(ref buffer, formatCode, FormatCode.Symbol8, FormatCode.Symbol32, "symbol");
        }

        /// <summary>
        /// Reads a list value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static List ReadList(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            int size;
            int count;
            if (formatCode == FormatCode.List0)
            {
                size = count = 0;
            }
            else if (formatCode == FormatCode.List8)
            {
                size = AmqpBitConverter.ReadUByte(ref buffer);
                count = AmqpBitConverter.ReadUByte(ref buffer);
            }
            else if (formatCode == FormatCode.List32)
            {
                size = (int)AmqpBitConverter.ReadUInt(buffer);
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }

            List value = new List();
            for (int i = 0; i < count; ++i)
            {
                value.Add(ReadObject(ref buffer));
            }

            return value;
        }

        /// <summary>
        /// Reads an array value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static Array ReadArray(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            int size;
            int count;
            if (formatCode == FormatCode.Array8)
            {
                size = AmqpBitConverter.ReadUByte(ref buffer);
                count = AmqpBitConverter.ReadUByte(ref buffer);
            }
            else if (formatCode == FormatCode.Array32)
            {
                size = (int)AmqpBitConverter.ReadUInt(buffer);
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }

            formatCode = Encoder.ReadFormatCode(ref buffer);

            Array value = null;
            IList list = null;
            for (int i = 0; i < count; ++i)
            {
                if (value == null)
                {
                    // decode object
                    object obj = Decode(ref buffer, formatCode);

                    // create array
                    Array.CreateInstance(typeof(object), count);
                    list = value;
                }
                else
                {
                    list[i] = Decode(ref buffer, formatCode);
                }
            }

            return value;
        }

        /// <summary>
        /// Reads a map value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static Map ReadMap(ref ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            int size;
            int count;
            if (formatCode == FormatCode.Map8)
            {
                size = AmqpBitConverter.ReadUByte(ref buffer);
                count = AmqpBitConverter.ReadUByte(ref buffer);
            }
            else if (formatCode == FormatCode.Map32)
            {
                size = (int)AmqpBitConverter.ReadUInt(buffer);
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }

            if (count % 2 > 0)
            {
                throw new AmqpException(ErrorCode.InvalidMapCount, count.ToString());
            }

            Map value = new Map();
            for (int i = 0; i < count; i += 2)
            {
                value.Add(ReadObject(ref buffer), ReadObject(ref buffer));
            }

            return value;
        }

        static string ReadString(ref ByteBuffer buffer, byte formatCode, byte code8, byte code32, string type)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            int count;
            if (formatCode == code8)
            {
                count = AmqpBitConverter.ReadUByte(ref buffer);
            }
            else if (formatCode == code32)
            {
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }

            buffer.Validate(false, count);
            string value = new string(Encoding.UTF8.GetChars(buffer.Buffer, buffer.Offset, count));
            buffer.Complete(count);

            return value;
        }

        static AmqpException DecodeException(byte formatCode, int offset)
        {
            return new AmqpException(ErrorCode.DecodeError, formatCode + " :: " + offset);
        }

        static object Decode(ref ByteBuffer buffer, byte formatCode)
        {
            switch (formatCode)
            {

                case 0x40:
                    // null
                    return null;  //0

                case 0x41:
                //boolean.true
                case 0x42:
                //boolean.false
                case 0x56:
                    //boolean
                    return ReadBoolean(ref buffer, formatCode); //1

                case 0x50:
                    // ubyte
                    return ReadUByte(ref buffer, formatCode); //2

                case 0x60:
                    // ushort
                    return ReadUShort(ref buffer, formatCode); //3

                case 0x43:
                // uint0
                case 0x52:
                // small.uint
                case 0x70:
                    // uint
                    return ReadUInt(ref buffer, formatCode);  //4 

                case 0x44:
                // ulong0
                case 0x53:
                // small.ulong
                case 0x80:
                    // ulong
                    return ReadULong(ref buffer, formatCode);  //5

                case 0x51:
                    // byte
                    return ReadByte(ref buffer, formatCode);  //6
                    break;

                case 0x61:
                    // short
                    return ReadShort(ref buffer, formatCode);  //7

                case 0x54:
                // small.int
                case 0x71:
                    // int
                    return ReadInt(ref buffer, formatCode);  //8

                case 0x55:
                // small.long
                case 0x81:
                    // long
                    return ReadLong(ref buffer, formatCode);  //9

                case 0x72:
                    // float
                    return ReadFloat(ref buffer, formatCode);  //10

                case 0x82:
                    // double
                    return ReadDouble(ref buffer, formatCode);  //11

                case 0x73:
                    // char
                    return ReadChar(ref buffer, formatCode);  //12

                case 0x83:
                    // timestamp
                    return ReadTimestamp(ref buffer, formatCode);  //13

                case 0x98:
                    // uuid
                    return ReadUuid(ref buffer, formatCode);  //14

                case 0xa0:
                // bin8
                case 0xb0:
                    // bin32
                    return ReadBinary(ref buffer, formatCode);  //15

                case 0x45:
                // list0
                case 0xa1:
                // str8
                case 0xb1:
                    // str32
                    return ReadString(ref buffer, formatCode);  //16

                case 0xa3:
                // sym8
                case 0xb3:
                    // sym32
                    return ReadSymbol(ref buffer, formatCode);  //17

                case 0xc0:
                // list8
                case 0xd0:
                    // list32
                    return ReadList(ref buffer, formatCode);  //18

                case 0xc1:
                // map8
                case 0xd1:
                    // map32
                    return ReadMap(ref buffer, formatCode);  //19

                case 0xe0:
                // array8
                case 0xf0:
                    // array32
                    return ReadArray(ref buffer, formatCode);  //20

                default:
                    // 0x74:decimal32
                    // 0x84:decimal64
                    throw DecodeException(formatCode, buffer.Offset);
            }
        }
    }

#else
    delegate void Encode(ByteBuffer buffer, object value, bool smallEncoding);

    delegate object Decode(ByteBuffer buffer, byte formatCode);

    /// <summary>
    /// Encodes or decodes AMQP types.
    /// </summary>
    public static class Encoder
    {
        class Serializer
        {
            public Type Type;
            public Encode Encoder;
            public Decode Decoder;
        }

#if NETMF
        // NETMF DateTime ticks origin is 1601/1/1
        const long epochTicks = 116444736000000000; // 1970-1-1 00:00:00 UTC
#else
        const long epochTicks = 621355968000000000; // 1970-1-1 00:00:00 UTC
#endif
        const long ticksPerMillisecond = 10000;

        static Serializer[] serializers;
        static Map codecByType;
        static byte[][] codecIndexTable;
        static Map knownDescrided;

        static Encoder()
        {
            if (serializers == null)
            {
                Initialize();
            }
        }

        internal static void Initialize()
        {
            knownDescrided = new Map();

            serializers = new Serializer[]
            {
                // 0: null
                new Serializer()
                {
                    Type = null,
                    Encoder = delegate(ByteBuffer b, object o, bool s) { AmqpBitConverter.WriteUByte(b, FormatCode.Null); },
                    Decoder = delegate(ByteBuffer b, byte c) { return null; }
                },
                // 1: boolean
                new Serializer()
                {
                    Type = typeof(bool),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteBoolean(b, (bool)o, s); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadBoolean(b, c); }
                },
                // 2: ubyte
                new Serializer()
                {
                    Type = typeof(byte),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteUByte(b, (byte)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadUByte(b, c); }
                },
                // 3: ushort
                new Serializer()
                {
                    Type = typeof(ushort),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteUShort(b, (ushort)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadUShort(b, c); }
                },
                // 4: uint
                new Serializer()
                {
                    Type = typeof(uint),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteUInt(b, (uint)o, s); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadUInt(b, c); }
                },
                // 5: ulong
                new Serializer()
                {
                    Type = typeof(ulong),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteULong(b, (ulong)o, s); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadULong(b, c); }
                },
                // 6: byte
                new Serializer()
                {
                    Type = typeof(sbyte),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteByte(b, (sbyte)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadByte(b, c); }
                },
                // 7: short
                new Serializer()
                {
                    Type = typeof(short),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteShort(b, (short)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadShort(b, c); }
                },
                // 8: int
                new Serializer()
                {
                    Type = typeof(int),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteInt(b, (int)o, s); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadInt(b, c); }
                },
                // 9: long
                new Serializer()
                {
                    Type = typeof(long),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteLong(b, (long)o, s); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadLong(b, c); }
                },
                // 10: float
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteFloat(b, (float)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadFloat(b, c); }
                },
                // 11: double
                new Serializer()
                {
                    Type = typeof(double),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteDouble(b, (double)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadDouble(b, c); }
                },
                // 12: char
                new Serializer()
                {
                    Type = typeof(char),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteChar(b, (char)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadChar(b, c); }
                },
                // 13: timestamp
                new Serializer()
                {
                    Type = typeof(DateTime),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteTimestamp(b, (DateTime)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadTimestamp(b, c); }
                },
                // 14: uuid
                new Serializer()
                {
                    Type = typeof(Guid),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteUuid(b, (Guid)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadUuid(b, c); }
                },
                // 15: binary
                new Serializer()
                {
                    Type = typeof(byte[]),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteBinary(b, (byte[])o, s); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadBinary(b, c); }
                },
                // 16: string
                new Serializer()
                {
                    Type = typeof(string),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteString(b, (string)o, s); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadString(b, c); }
                },
                // 17: symbol
                new Serializer()
                {
                    Type = typeof(Symbol),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteSymbol(b, (Symbol)o, s); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadSymbol(b, c); }
                },
                // 18: list
                new Serializer()
                {
                    Type = typeof(List),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteList(b, (IList)o, s); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadList(b, c); }
                },
                // 19: map
                new Serializer()
                {
                    Type = typeof(Map),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteMap(b, (Map)o, s); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadMap(b, c); }
                },
                // 20: array
                new Serializer()
                {
                    Type = typeof(Array),
                    Encoder = delegate(ByteBuffer b, object o, bool s) { WriteArray(b, (Array)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadArray(b, c); }
                },
                // 21: invalid
                null
            };

            codecByType = new Map()
            {
                { typeof(bool),     serializers[1] },
                { typeof(byte),     serializers[2] },
                { typeof(ushort),   serializers[3] },
                { typeof(uint),     serializers[4] },
                { typeof(ulong),    serializers[5] },
                { typeof(sbyte),    serializers[6] },
                { typeof(short),    serializers[7] },
                { typeof(int),      serializers[8] },
                { typeof(long),     serializers[9] },
                { typeof(float),    serializers[10] },
                { typeof(double),   serializers[11] },
                { typeof(char),     serializers[12] },
                { typeof(DateTime), serializers[13] },
                { typeof(Guid),     serializers[14] },
                { typeof(byte[]),   serializers[15] },
                { typeof(string),   serializers[16] },
                { typeof(Symbol),   serializers[17] },
                { typeof(List),     serializers[18] },
                { typeof(Map),      serializers[19] },
                { typeof(Fields),   serializers[19] },
            };

            codecIndexTable = new byte[][]
            {
                // 0x40:null, 0x41:boolean.true, 0x42:boolean.false, 0x43:uint0, 0x44:ulong0, 0x45:list0
                new byte[] { 0, 1, 1, 4, 5, 18 },

                // 0x50:ubyte, 0x51:byte, 0x52:small.uint, 0x53:small.ulong, 0x54:small.int, 0x55:small.long, 0x56:boolean
                new byte[] { 2, 6, 4, 5, 8, 9, 1 },

                // 0x60:ushort, 0x61:short
                new byte[] { 3, 7 },

                // 0x70:uint, 0x71:int, 0x72:float, 0x73:char, 0x74:decimal32
                new byte[] { 4, 8, 10, 12 },

                // 0x80:ulong, 0x81:long, 0x82:double, 0x83:timestamp, 0x84:decimal64
                new byte[] { 5, 9, 11, 13 },

                // 0x98:uuid
                new byte[] { 21, 21, 21, 21, 21, 21, 21, 21, 14 },
            
                // 0xa0:bin8, 0xa1:str8, 0xa3:sym8
                new byte[] { 15, 16, 21, 17 },

                // 0xb0:bin32, 0xb1:str32, 0xb3:sym32
                new byte[] { 15, 16, 21, 17 },

                // 0xc0:list8, 0xc1:map8
                new byte[] { 18, 19 },

                // 0xd0:list32, 0xd1:map32
                new byte[] { 18, 19 },

                // 0xe0:array8
                new byte[] { 20 },

                // 0xf0:array32
                new byte[] { 20 }
            };
        }

        internal static bool TryGetCodec(Type type, out Encode encoder, out Decode decoder)
        {
            Serializer codec = (Serializer)codecByType[type];
            if (codec == null && type.IsArray)
            {
                codec = serializers[20];
            }

            if (codec != null)
            {
                encoder = codec.Encoder;
                decoder = codec.Decoder;
                return true;
            }
            else
            {
                encoder = null;
                decoder = null;
                return false;
            }
        }

        /// <summary>
        /// Adds a factory for a custom described type, usually for decoding.
        /// </summary>
        /// <param name="descriptor">The descriptor of the type.</param>
        /// <param name="ctor">The delegate to invoke to create the object.</param>
        public static void AddKnownDescribed(Descriptor descriptor, CreateDescribed ctor)
        {
            lock (knownDescrided)
            {
                knownDescrided.Add(descriptor.Name, ctor);
                knownDescrided.Add(descriptor.Code, ctor);
            }
        }

        /// <summary>
        /// Converts a DateTime value to AMQP timestamp (milliseconds from Unix epoch)
        /// </summary>
        /// <param name="dateTime">The DateTime value to convert.</param>
        /// <returns></returns>
        public static long DateTimeToTimestamp(DateTime dateTime)
        {
            return (long)((dateTime.ToUniversalTime().Ticks - epochTicks) / ticksPerMillisecond);
        }

        /// <summary>
        /// Converts an AMQP timestamp ((milliseconds from Unix epoch)) to a DateTime.
        /// </summary>
        /// <param name="timestamp">The AMQP timestamp to convert.</param>
        /// <returns></returns>
        public static DateTime TimestampToDateTime(long timestamp)
        {
            return new DateTime(epochTicks + timestamp * ticksPerMillisecond, DateTimeKind.Utc);
        }

        internal static byte ReadFormatCode(ByteBuffer buffer)
        {
            return AmqpBitConverter.ReadUByte(buffer);
        }

        /// <summary>
        /// Writes an AMQP object to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The AMQP value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteObject(ByteBuffer buffer, object value, bool smallEncoding = true)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Null);
            }
            else
            {
                Encode encoder;
                Decode decoder;
                if (TryGetCodec(value.GetType(), out encoder, out decoder))
                {
                    encoder(buffer, value, smallEncoding);
                }
                else if (value is Described)
                {
                    ((Described)value).Encode(buffer);
                }
                else
                {
                    throw new AmqpException(ErrorCode.NotImplemented,
                        Fx.Format(SRAmqp.EncodingTypeNotSupported, value.GetType()));
                }
            }
        }

        /// <summary>
        /// Writes a boolean value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The boolean value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteBoolean(ByteBuffer buffer, bool value, bool smallEncoding)
        {
            if (smallEncoding)
            {
                AmqpBitConverter.WriteUByte(buffer, value ? FormatCode.BooleanTrue : FormatCode.BooleanFalse);
            }
            else
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Boolean);
                AmqpBitConverter.WriteUByte(buffer, (byte)(value ? 1 : 0));
            }
        }

        /// <summary>
        /// Writes an unsigned byte value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The unsigned byte value.</param>
        public static void WriteUByte(ByteBuffer buffer, byte value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.UByte);
            AmqpBitConverter.WriteUByte(buffer, value);
        }

        /// <summary>
        /// Writes an unsigned 16-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The unsigned 16-bit integer value.</param>
        public static void WriteUShort(ByteBuffer buffer, ushort value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.UShort);
            AmqpBitConverter.WriteUShort(buffer, value);
        }

        /// <summary>
        /// Writes an unsigned 32-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The unsigned 32-bit integer value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteUInt(ByteBuffer buffer, uint value, bool smallEncoding)
        {
            if (!smallEncoding || value > byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.UInt);
                AmqpBitConverter.WriteUInt(buffer, value);
            }
            else if (value == 0)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.UInt0);
            }
            else if (value <= byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.SmallUInt);
                AmqpBitConverter.WriteUByte(buffer, (byte)value);
            }
        }

        /// <summary>
        /// Writes an unsigned 64-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The unsigned 64-bit integer value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteULong(ByteBuffer buffer, ulong value, bool smallEncoding)
        {
            if (!smallEncoding || value > byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.ULong);
                AmqpBitConverter.WriteULong(buffer, value);
            }
            else if (value == 0)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.ULong0);
            }
            else if (value <= byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.SmallULong);
                AmqpBitConverter.WriteUByte(buffer, (byte)value);
            }
        }

        /// <summary>
        /// Writes a signed byte value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The signed byte value.</param>
        public static void WriteByte(ByteBuffer buffer, sbyte value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Byte);
            AmqpBitConverter.WriteByte(buffer, value);
        }

        /// <summary>
        /// Writes a signed 16-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The signed 16-bit integer value.</param>
        public static void WriteShort(ByteBuffer buffer, short value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Short);
            AmqpBitConverter.WriteShort(buffer, value);
        }

        /// <summary>
        /// Writes a signed 32-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The signed 32-bit integer value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteInt(ByteBuffer buffer, int value, bool smallEncoding)
        {
            if (smallEncoding && value >= sbyte.MinValue && value <= sbyte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.SmallInt);
                AmqpBitConverter.WriteByte(buffer, (sbyte)value);
            }
            else
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Int);
                AmqpBitConverter.WriteInt(buffer, value);
            }
        }

        /// <summary>
        /// Writes a signed 64-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The signed 64-bit integer value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteLong(ByteBuffer buffer, long value, bool smallEncoding)
        {
            if (smallEncoding && value >= sbyte.MinValue && value <= sbyte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.SmallLong);
                AmqpBitConverter.WriteByte(buffer, (sbyte)value);
            }
            else
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Long);
                AmqpBitConverter.WriteLong(buffer, value);
            }
        }

        /// <summary>
        /// Writes a char value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The char value.</param>
        public static void WriteChar(ByteBuffer buffer, char value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Char);
            AmqpBitConverter.WriteInt(buffer, value);   // TODO: utf32
        }

        /// <summary>
        /// Writes a 32-bit floating-point value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The 32-bit floating-point value.</param>
        public static void WriteFloat(ByteBuffer buffer, float value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Float);
            AmqpBitConverter.WriteFloat(buffer, value);
        }

        /// <summary>
        /// Writes a 64-bit floating-point value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The 64-bit floating-point value.</param>
        public static void WriteDouble(ByteBuffer buffer, double value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Double);
            AmqpBitConverter.WriteDouble(buffer, value);
        }

        /// <summary>
        /// Writes a timestamp value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The timestamp value which is the milliseconds since UNIX epoch.</param>
        public static void WriteTimestamp(ByteBuffer buffer, DateTime value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.TimeStamp);
            AmqpBitConverter.WriteLong(buffer, DateTimeToTimestamp(value));
        }

        /// <summary>
        /// Writes a uuid value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The uuid value.</param>
        public static void WriteUuid(ByteBuffer buffer, Guid value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Uuid);
            AmqpBitConverter.WriteUuid(buffer, value);
        }

        /// <summary>
        /// Writes a binary value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The binary value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteBinary(ByteBuffer buffer, byte[] value, bool smallEncoding)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Null);
            }
            else if (smallEncoding && value.Length <= byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Binary8);
                AmqpBitConverter.WriteUByte(buffer, (byte)value.Length);
                AmqpBitConverter.WriteBytes(buffer, value, 0, value.Length);
            }
            else
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Binary32);
                AmqpBitConverter.WriteUInt(buffer, (uint)value.Length);
                AmqpBitConverter.WriteBytes(buffer, value, 0, value.Length);
            }
        }

        /// <summary>
        /// Writes a string value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The string value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteString(ByteBuffer buffer, string value, bool smallEncoding)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Null);
            }
            else
            {
                byte[] data = Encoding.UTF8.GetBytes(value);
                if (smallEncoding && data.Length <= byte.MaxValue)
                {
                    AmqpBitConverter.WriteUByte(buffer, FormatCode.String8Utf8);
                    AmqpBitConverter.WriteUByte(buffer, (byte)data.Length);
                    AmqpBitConverter.WriteBytes(buffer, data, 0, data.Length);
                }
                else
                {
                    AmqpBitConverter.WriteUByte(buffer, FormatCode.String32Utf8);
                    AmqpBitConverter.WriteUInt(buffer, (uint)data.Length);
                    AmqpBitConverter.WriteBytes(buffer, data, 0, data.Length);
                }
            }
        }

        /// <summary>
        /// Writes a symbol value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The symbol value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteSymbol(ByteBuffer buffer, Symbol value, bool smallEncoding)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Null);
            }
            else
            {
                byte[] data = Encoding.UTF8.GetBytes(value);
                if (smallEncoding && data.Length <= byte.MaxValue)
                {
                    AmqpBitConverter.WriteUByte(buffer, FormatCode.Symbol8);
                    AmqpBitConverter.WriteUByte(buffer, (byte)data.Length);
                    AmqpBitConverter.WriteBytes(buffer, data, 0, data.Length);
                }
                else
                {
                    AmqpBitConverter.WriteUByte(buffer, FormatCode.Symbol32);
                    AmqpBitConverter.WriteUInt(buffer, (uint)data.Length);
                    AmqpBitConverter.WriteBytes(buffer, data, 0, data.Length);
                }
            }
        }

        /// <summary>
        /// Writes a list value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The list value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteList(ByteBuffer buffer, IList value, bool smallEncoding)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Null);
            }
            else
            {
                // trim tailing nulls
                int last = value.Count - 1;
                while (last >= 0 && value[last] == null)
                {
                    --last;
                }

                if (last < 0 && smallEncoding)
                {
                    AmqpBitConverter.WriteUByte(buffer, FormatCode.List0);
                }
                else
                {
                    int pos = buffer.WritePos;
                    AmqpBitConverter.WriteUByte(buffer, 0);
                    AmqpBitConverter.WriteUInt(buffer, 0);
                    AmqpBitConverter.WriteUInt(buffer, 0);

                    for (int i = 0; i <= last; ++i)
                    {
                        Encoder.WriteObject(buffer, value[i], smallEncoding);
                    }

                    int size = buffer.WritePos - pos - 9;
                    int count = last + 1;

                    if (smallEncoding && size < byte.MaxValue && count <= byte.MaxValue)
                    {
                        buffer.Buffer[pos] = FormatCode.List8;
                        buffer.Buffer[pos + 1] = (byte)(size + 1);
                        buffer.Buffer[pos + 2] = (byte)count;
                        Array.Copy(buffer.Buffer, pos + 9, buffer.Buffer, pos + 3, size);
                        buffer.Shrink(6);
                    }
                    else
                    {
                        buffer.Buffer[pos] = FormatCode.List32;
                        AmqpBitConverter.WriteInt(buffer.Buffer, pos + 1, size + 4);
                        AmqpBitConverter.WriteInt(buffer.Buffer, pos + 5, count);
                    }
                }
            }
        }

        /// <summary>
        /// Writes an array value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The array value.</param>
        public static void WriteArray(ByteBuffer buffer, Array value)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Null);
            }
            else
            {
                int count = value.Length;
                Fx.Assert(count > 0, "must have at least 1 element in array");
                int pos = buffer.WritePos;
                AmqpBitConverter.WriteUByte(buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);

                for (int i = 0; i < count; ++i)
                {
                    object item = value.GetValue(i);
                    if (i == 0)
                    {
                        Encoder.WriteObject(buffer, item, false);
                    }
                    else
                    {
                        int lastPos = buffer.WritePos - 1;
                        byte lastByte = buffer.Buffer[lastPos];
                        buffer.Shrink(1);
                        Encoder.WriteObject(buffer, item, false);
                        buffer.Buffer[lastPos] = lastByte;
                    }
                }

                int size = buffer.WritePos - pos - 9;

                if (size < byte.MaxValue && count <= byte.MaxValue)
                {
                    buffer.Buffer[pos] = FormatCode.Array8;
                    buffer.Buffer[pos + 1] = (byte)(size + 1);
                    buffer.Buffer[pos + 2] = (byte)count;
                    Array.Copy(buffer.Buffer, pos + 9, buffer.Buffer, pos + 3, size);
                    buffer.Shrink(6);
                }
                else
                {
                    buffer.Buffer[pos] = FormatCode.Array32;
                    AmqpBitConverter.WriteInt(buffer.Buffer, pos + 1, size + 4);
                    AmqpBitConverter.WriteInt(buffer.Buffer, pos + 5, count);
                }
            }
        }

        /// <summary>
        /// Writes a map value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The map value.</param>
        /// <param name="smallEncoding">if true, try using small encoding if possible.</param>
        public static void WriteMap(ByteBuffer buffer, Map value, bool smallEncoding)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Null);
            }
            else
            {
                int pos = buffer.WritePos;
                AmqpBitConverter.WriteUByte(buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);

                foreach (var key in value.Keys)
                {
                    Encoder.WriteObject(buffer, key);
                    Encoder.WriteObject(buffer, value[key]);
                }

                int size = buffer.WritePos - pos - 9;
                int count = value.Count * 2;

                if (smallEncoding && size < byte.MaxValue && count <= byte.MaxValue)
                {
                    buffer.Buffer[pos] = FormatCode.Map8;
                    buffer.Buffer[pos + 1] = (byte)(size + 1);
                    buffer.Buffer[pos + 2] = (byte)count;
                    Array.Copy(buffer.Buffer, pos + 9, buffer.Buffer, pos + 3, size);
                    buffer.Shrink(6);
                }
                else
                {
                    buffer.Buffer[pos] = FormatCode.Map32;
                    AmqpBitConverter.WriteInt(buffer.Buffer, pos + 1, size + 4);
                    AmqpBitConverter.WriteInt(buffer.Buffer, pos + 5, count);
                }
            }
        }

        /// <summary>
        /// Reads an object from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        public static object ReadObject(ByteBuffer buffer)
        {
            byte formatCode = Encoder.ReadFormatCode(buffer);
            return ReadObject(buffer, formatCode);
        }

        /// <summary>
        /// Reads an object from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        /// <returns></returns>
        public static object ReadObject(ByteBuffer buffer, byte formatCode)
        {
            Serializer serializer = GetSerializer(formatCode);
            if (serializer != null)
            {
                return serializer.Decoder(buffer, formatCode);
            }

            if (formatCode == FormatCode.Described)
            {
                return ReadDescribed(buffer, formatCode);
            }

            throw DecodeException(formatCode, buffer.Offset);
        }

        /// <summary>
        /// Reads a described value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static object ReadDescribed(ByteBuffer buffer, byte formatCode)
        {
            Fx.Assert(formatCode == FormatCode.Described, "Format code must be described (0)");
            Described described;
            object descriptor = Encoder.ReadObject(buffer);
            CreateDescribed create = null;
            if ((create = (CreateDescribed)knownDescrided[descriptor]) == null)
            {
                object value = Encoder.ReadObject(buffer);
                described = new DescribedValue(descriptor, value);
            }
            else
            {
                described = create();
                described.DecodeValue(buffer);
            }

            return described;
        }

        /// <summary>
        /// Reads a boolean value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static bool ReadBoolean(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.BooleanTrue)
            {
                return true;
            }
            else if (formatCode == FormatCode.BooleanFalse)
            {
                return false;
            }
            else if (formatCode == FormatCode.Boolean)
            {
                byte data = AmqpBitConverter.ReadUByte(buffer);
                return data != 0;
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads an unsigned byte value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static byte ReadUByte(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.UByte)
            {
                return AmqpBitConverter.ReadUByte(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads an unsigned 16-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static ushort ReadUShort(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.UShort)
            {
                return AmqpBitConverter.ReadUShort(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads an unsigned 32-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static uint ReadUInt(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.UInt0)
            {
                return 0;
            }
            else if (formatCode == FormatCode.SmallUInt)
            {
                return AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == FormatCode.UInt)
            {
                return AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads an unsigned 64-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static ulong ReadULong(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.ULong0)
            {
                return 0;
            }
            else if (formatCode == FormatCode.SmallULong)
            {
                return AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == FormatCode.ULong)
            {
                return AmqpBitConverter.ReadULong(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a signed byte from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static sbyte ReadByte(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Byte)
            {
                return AmqpBitConverter.ReadByte(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a signed 16-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static short ReadShort(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Short)
            {
                return AmqpBitConverter.ReadShort(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a signed 32-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static int ReadInt(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.SmallInt)
            {
                return AmqpBitConverter.ReadByte(buffer);
            }
            else if (formatCode == FormatCode.Int)
            {
                return AmqpBitConverter.ReadInt(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a signed 64-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static long ReadLong(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.SmallLong)
            {
                return AmqpBitConverter.ReadByte(buffer);
            }
            else if (formatCode == FormatCode.Long)
            {
                return AmqpBitConverter.ReadLong(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a char value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static char ReadChar(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Char)
            {
                return (char)AmqpBitConverter.ReadInt(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a 32-bit floating-point value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static float ReadFloat(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Float)
            {
                return AmqpBitConverter.ReadFloat(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a 64-bit floating-point value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static double ReadDouble(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Double)
            {
                return AmqpBitConverter.ReadDouble(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a timestamp value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static DateTime ReadTimestamp(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.TimeStamp)
            {
                return TimestampToDateTime(AmqpBitConverter.ReadLong(buffer));
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a uuid value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static Guid ReadUuid(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Uuid)
            {
                return AmqpBitConverter.ReadUuid(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }
        }

        /// <summary>
        /// Reads a binary value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static byte[] ReadBinary(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            int count;
            if (formatCode == FormatCode.Binary8)
            {
                count = AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == FormatCode.Binary32)
            {
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }

            buffer.Validate(false, count);
            byte[] value = new byte[count];
            Array.Copy(buffer.Buffer, buffer.Offset, value, 0, count);
            buffer.Complete(count);

            return value;
        }

        /// <summary>
        /// Reads a string value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static string ReadString(ByteBuffer buffer, byte formatCode)
        {
            return ReadString(buffer, formatCode, FormatCode.String8Utf8, FormatCode.String32Utf8, "string");
        }

        /// <summary>
        /// Reads a symbol value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static Symbol ReadSymbol(ByteBuffer buffer, byte formatCode)
        {
            return ReadString(buffer, formatCode, FormatCode.Symbol8, FormatCode.Symbol32, "symbol");
        }

        /// <summary>
        /// Reads a list value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static List ReadList(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }
            
            int size;
            int count;
            if (formatCode == FormatCode.List0)
            {
                size = count = 0;
            }
            else if (formatCode == FormatCode.List8)
            {
                size = AmqpBitConverter.ReadUByte(buffer);
                count = AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == FormatCode.List32)
            {
                size = (int)AmqpBitConverter.ReadUInt(buffer);
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }

            List value = new List();
            for (int i = 0; i < count; ++i)
            {
                value.Add(ReadObject(buffer));
            }

            return value;
        }

        /// <summary>
        /// Reads an array value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static Array ReadArray(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            int size;
            int count;
            if (formatCode == FormatCode.Array8)
            {
                size = AmqpBitConverter.ReadUByte(buffer);
                count = AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == FormatCode.Array32)
            {
                size = (int)AmqpBitConverter.ReadUInt(buffer);
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }

            formatCode = Encoder.ReadFormatCode(buffer);
            Serializer codec = GetSerializer(formatCode);
            if (codec == null)
            {
                throw DecodeException(formatCode, buffer.Offset);
            }

            Array value = Array.CreateInstance(codec.Type, count);
            IList list = value;
            for (int i = 0; i < count; ++i)
            {
                list[i] = codec.Decoder(buffer, formatCode);
            }

            return value;
        }

        /// <summary>
        /// Reads a map value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static Map ReadMap(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            int size;
            int count;
            if (formatCode == FormatCode.Map8)
            {
                size = AmqpBitConverter.ReadUByte(buffer);
                count = AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == FormatCode.Map32)
            {
                size = (int)AmqpBitConverter.ReadUInt(buffer);
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }

            if (count % 2 > 0)
            {
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.InvalidMapCount, count));
            }

            Map value = new Map();
            for (int i = 0; i < count; i += 2)
            {
                value.Add(ReadObject(buffer), ReadObject(buffer));
            }

            return value;
        }

        static Serializer GetSerializer(byte formatCode)
        {
            int type = ((formatCode & 0xF0) >> 4) - 4;
            if (type >= 0 && type < codecIndexTable.Length)
            {
                int index = formatCode & 0x0F;
                if (index < codecIndexTable[type].Length)
                {
                    return serializers[codecIndexTable[type][index]];
                }
            }

            return null;
        }

        static string ReadString(ByteBuffer buffer, byte formatCode, byte code8, byte code32, string type)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            int count;
            if (formatCode == code8)
            {
                count = AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == code32)
            {
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw DecodeException(formatCode, buffer.Offset);
            }

            buffer.Validate(false, count);
            string value = new string(Encoding.UTF8.GetChars(buffer.Buffer, buffer.Offset, count));
            buffer.Complete(count);

            return value;
        }

        static AmqpException DecodeException(byte formatCode, int offset)
        {
            return new AmqpException(ErrorCode.DecodeError,
                Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, offset));
        }

    }
#endif

}
