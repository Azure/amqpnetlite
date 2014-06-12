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

    public delegate Described CreateDescribed();

    static class Encoder
    {
        delegate void Encode(ByteBuffer buffer, object value);
        delegate object Decode(ByteBuffer buffer, byte formatCode);

        public static readonly DateTime StartOfEpoch = new DateTime(1970, 1, 1, 0, 0, 0);

        static readonly Map encoders = new Map()
        {
            { typeof(bool),     (Encode)((b, o) => WriteBoolean(b, (bool)o)) },
            { typeof(byte),     (Encode)((b, o) => WriteUByte(b, (byte)o)) },
            { typeof(ushort),   (Encode)((b, o) => WriteUShort(b, (ushort)o)) },
            { typeof(uint),     (Encode)((b, o) => WriteUInt(b, (uint)o)) },
            { typeof(ulong),    (Encode)((b, o) => WriteULong(b, (ulong)o)) },
            { typeof(sbyte),    (Encode)((b, o) => WriteByte(b, (sbyte)o)) },
            { typeof(short),    (Encode)((b, o) => WriteShort(b, (short)o)) },
            { typeof(int),      (Encode)((b, o) => WriteInt(b, (int)o)) },
            { typeof(long),     (Encode)((b, o) => WriteLong(b, (long)o)) },
            { typeof(float),    (Encode)((b, o) => WriteFloat(b, (float)o)) },
            { typeof(double),   (Encode)((b, o) => WriteDouble(b, (double)o)) },
            { typeof(char),     (Encode)((b, o) => WriteChar(b, (char)o)) },
            { typeof(DateTime), (Encode)((b, o) => WriteTimestamp(b, (DateTime)o)) },
            { typeof(Guid),     (Encode)((b, o) => WriteUuid(b, (Guid)o)) },
            { typeof(byte[]),   (Encode)((b, o) => WriteBinary(b, (byte[])o)) },
            { typeof(string),   (Encode)((b, o) => WriteString(b, (string)o)) },
            { typeof(Symbol),   (Encode)((b, o) => WriteSymbol(b, (Symbol)o)) },
            { typeof(List),     (Encode)((b, o) => WriteList(b, (IList)o)) },
            { typeof(Map),      (Encode)((b, o) => WriteMap(b, (Map)o)) },
            { typeof(Multiple), (Encode)((b, o) => ((Multiple)o).Encode(b)) },
        };

        static readonly Map decoders = new Map()
        {
            { FormatCode.Null,      (Decode)((b, c) => null) },
            { FormatCode.Boolean,   (Decode)((b, c) => ReadBoolean(b, c)) },
            { FormatCode.BooleanTrue,   (Decode)((b, c) => true) },
            { FormatCode.BooleanFalse,  (Decode)((b, c) => false) },
            { FormatCode.UByte,     (Decode)((b, c) => ReadUByte(b, c)) },
            { FormatCode.UShort,    (Decode)((b, c) => ReadUShort(b, c)) },
            { FormatCode.UInt0,     (Decode)((b, c) => 0u) },
            { FormatCode.SmallUInt, (Decode)((b, c) => ReadUInt(b, c)) },
            { FormatCode.UInt,      (Decode)((b, c) => ReadUInt(b, c)) },
            { FormatCode.ULong0,    (Decode)((b, c) => 0ul) },
            { FormatCode.SmallULong,(Decode)((b, c) => ReadULong(b, c)) },
            { FormatCode.ULong,     (Decode)((b, c) => ReadULong(b, c)) },
            { FormatCode.Byte,      (Decode)((b, c) => ReadByte(b, c)) },
            { FormatCode.Short,     (Decode)((b, c) => ReadShort(b, c)) },
            { FormatCode.Int,       (Decode)((b, c) => ReadInt(b, c)) },
            { FormatCode.SmallInt,  (Decode)((b, c) => ReadInt(b, c)) },
            { FormatCode.Long,      (Decode)((b, c) => ReadLong(b, c)) },
            { FormatCode.SmallLong, (Decode)((b, c) => ReadLong(b, c)) },
            { FormatCode.Float,     (Decode)((b, c) => ReadFloat(b, c)) },
            { FormatCode.Double,    (Decode)((b, c) => ReadDouble(b, c)) },
            { FormatCode.Char,      (Decode)((b, c) => ReadChar(b, c)) },
            { FormatCode.TimeStamp, (Decode)((b, c) => ReadTimestamp(b, c)) },
            { FormatCode.Uuid,      (Decode)((b, c) => ReadUuid(b, c)) },
            { FormatCode.Binary8,   (Decode)((b, c) => ReadBinary(b, c)) },
            { FormatCode.Binary32,  (Decode)((b, c) => ReadBinary(b, c)) },
            { FormatCode.String8Utf8,  (Decode)((b, c) => ReadString(b, c)) },
            { FormatCode.String32Utf8, (Decode)((b, c) => ReadString(b, c)) },
            { FormatCode.Symbol8,   (Decode)((b, c) => ReadSymbol(b, c)) },
            { FormatCode.Symbol32,  (Decode)((b, c) => ReadSymbol(b, c)) },
            { FormatCode.List0,     (Decode)((b, c) => new object[0]) },
            { FormatCode.List8,     (Decode)((b, c) => ReadList(b, c)) },
            { FormatCode.List32,    (Decode)((b, c) => ReadList(b, c)) },
            { FormatCode.Map8,      (Decode)((b, c) => ReadMap(b, c)) },
            { FormatCode.Map32,     (Decode)((b, c) => ReadMap(b, c)) },
            { FormatCode.Array8,    (Decode)((b, c) => ReadArray(b, c)) },
            { FormatCode.Array32,   (Decode)((b, c) => ReadArray(b, c)) },
        };

        static Map knownDescrided;

        public static void AddKnownDescribed(Descriptor descriptor, CreateDescribed ctor)
        {
            if (knownDescrided == null) knownDescrided = new Map();

            lock (knownDescrided)
            {
                knownDescrided.Add(descriptor.Name, ctor);
                knownDescrided.Add(descriptor.Code, ctor);
            }
        }

        public static byte ReadFormatCode(ByteBuffer buffer)
        {
            return AmqpBitConverter.ReadUByte(buffer);
        }

        public static void WriteObject(ByteBuffer buffer, object value)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Null);
            }
            else
            {
                Encode encode = (Encode)encoders[value.GetType()];
                if (encode != null)
                {
                    encode(buffer, value);
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

        public static void WriteBoolean(ByteBuffer buffer, bool value)
        {
            AmqpBitConverter.WriteUByte(buffer, value ? FormatCode.BooleanTrue : FormatCode.BooleanFalse);
        }

        public static void WriteUByte(ByteBuffer buffer, byte value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.UByte);
            AmqpBitConverter.WriteUByte(buffer, value);
        }

        public static void WriteUShort(ByteBuffer buffer, ushort value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.UShort);
            AmqpBitConverter.WriteUShort(buffer, value);
        }

        public static void WriteUInt(ByteBuffer buffer, uint value)
        {
            if (value == 0)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.UInt0);
            }
            else if (value <= byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.SmallUInt);
                AmqpBitConverter.WriteUByte(buffer, (byte)value);
            }
            else
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.UInt);
                AmqpBitConverter.WriteUInt(buffer, value);
            }
        }

        public static void WriteULong(ByteBuffer buffer, ulong value)
        {
            if (value == 0)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.ULong0);
            }
            else if (value <= byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.SmallULong);
                AmqpBitConverter.WriteUByte(buffer, (byte)value);
            }
            else
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.ULong);
                AmqpBitConverter.WriteULong(buffer, value);
            }
        }

        public static void WriteByte(ByteBuffer buffer, sbyte value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Byte);
            AmqpBitConverter.WriteByte(buffer, value);
        }

        public static void WriteShort(ByteBuffer buffer, short value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Short);
            AmqpBitConverter.WriteShort(buffer, value);
        }

        public static void WriteInt(ByteBuffer buffer, int value)
        {
            if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
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

        public static void WriteLong(ByteBuffer buffer, long value)
        {
            if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
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

        public static void WriteChar(ByteBuffer buffer, char value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Char);
            AmqpBitConverter.WriteInt(buffer, value);   // TODO: utf32
        }

        public static void WriteFloat(ByteBuffer buffer, float value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Float);
            AmqpBitConverter.WriteFloat(buffer, value);
        }

        public static void WriteDouble(ByteBuffer buffer, double value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Double);
            AmqpBitConverter.WriteDouble(buffer, value);

        }

        public static void WriteTimestamp(ByteBuffer buffer, DateTime value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.TimeStamp);
            AmqpBitConverter.WriteLong(buffer, (value.ToUniversalTime() - StartOfEpoch).Ticks / TimeSpan.TicksPerMillisecond);
        }

        public static void WriteUuid(ByteBuffer buffer, Guid value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Uuid);
            AmqpBitConverter.WriteUuid(buffer, value);
        }

        public static void WriteBinary(ByteBuffer buffer, byte[] value)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Null);
            }
            else if (value.Length <= byte.MaxValue)
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

        public static void WriteString(ByteBuffer buffer, string value)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Null);
            }
            else
            {
                byte[] data = Encoding.UTF8.GetBytes(value);
                if (data.Length <= byte.MaxValue)
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

        public static void WriteSymbol(ByteBuffer buffer, Symbol value)
        {
            if (value.IsNull)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Null);
            }
            else
            {
                byte[] data = Encoding.UTF8.GetBytes(value);
                if (data.Length <= byte.MaxValue)
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

        public static void WriteList(ByteBuffer buffer, IList value)
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

                if (last < 0)
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
                        Encoder.WriteObject(buffer, value[i]);
                    }

                    int size = buffer.WritePos - pos - 9;
                    int count = last + 1;

                    if (size < byte.MaxValue && count <= byte.MaxValue)
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

        public static void WriteArray(ByteBuffer buffer, IList value)
        {
            if (value == null)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Null);
            }
            else
            {
                Fx.Assert(value.Count > 0, "must have at least 1 element in array");
                int pos = buffer.WritePos;
                AmqpBitConverter.WriteUByte(buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);

                Encoder.WriteObject(buffer, value[0]);
                for (int i = 1; i <= value.Count; ++i)
                {
                    int lastPos = buffer.WritePos - 1;
                    byte lastByte = buffer.Buffer[lastPos];
                    buffer.Shrink(1);
                    Encoder.WriteObject(buffer, value[i]);
                    buffer.Buffer[lastPos] = lastByte;
                }

                int size = buffer.WritePos - pos - 9;
                int count = value.Count;

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

        public static void WriteMap(ByteBuffer buffer, Map value)
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

                if (size < byte.MaxValue && count <= byte.MaxValue)
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

        public static object ReadObject(ByteBuffer buffer)
        {
            byte formatCode = Encoder.ReadFormatCode(buffer);
            Decode decode = (Decode)decoders[formatCode];
            if (decode != null)
            {
                return decode(buffer, formatCode);
            }
            else if (formatCode == FormatCode.Described)
            {
                Described described;
                object descriptor = Encoder.ReadObject(buffer);
                CreateDescribed create = (CreateDescribed)knownDescrided[descriptor];
                if (create == null)
                {
                    object value = Encoder.ReadObject(buffer);
                    Descriptor des = new Descriptor(descriptor is ulong ? (ulong)descriptor : 0, descriptor as string);
                    described = new DescribedValue(des) { Value = value };
                }
                else
                {
                    described = create();
                    described.DecodeValue(buffer);
                }

                return described;
            }
            else
            {
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "*"));
            }
        }

        public static bool ReadBoolean(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
            }

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
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "boolean"));
            }
        }

        public static byte ReadUByte(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
            }

            if (formatCode == FormatCode.UByte)
            {
                return AmqpBitConverter.ReadUByte(buffer);
            }
            else
            {
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "ubyte"));
            }
        }

        public static ushort ReadUShort(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
            }

            if (formatCode == FormatCode.UShort)
            {
                return AmqpBitConverter.ReadUShort(buffer);
            }
            else
            {
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "ushort"));
            }
        }

        public static uint ReadUInt(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
            }

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
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "uint"));
            }
        }

        public static ulong ReadULong(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
            }

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
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "ulong"));
            }
        }

        public static sbyte ReadByte(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
            }

            if (formatCode == FormatCode.Byte)
            {
                return AmqpBitConverter.ReadByte(buffer);
            }
            else
            {
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "byte"));
            }
        }

        public static short ReadShort(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
            }

            if (formatCode == FormatCode.Short)
            {
                return AmqpBitConverter.ReadShort(buffer);
            }
            else
            {
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "short"));
            }
        }

        public static int ReadInt(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
            }

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
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "int"));
            }
        }

        public static long ReadLong(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
            }

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
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "long"));
            }
        }

        public static char ReadChar(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
            }

            if (formatCode == FormatCode.Char)
            {
                return (char)AmqpBitConverter.ReadInt(buffer);
            }
            else
            {
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "char"));
            }
        }

        public static float ReadFloat(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
            }

            if (formatCode == FormatCode.Float)
            {
                return AmqpBitConverter.ReadFloat(buffer);
            }
            else
            {
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "float"));
            }
        }

        public static double ReadDouble(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
            }

            if (formatCode == FormatCode.Double)
            {
                return AmqpBitConverter.ReadDouble(buffer);
            }
            else
            {
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "double"));
            }
        }

        public static DateTime ReadTimestamp(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
            }

            if (formatCode == FormatCode.TimeStamp)
            {
                return StartOfEpoch.AddMilliseconds(AmqpBitConverter.ReadLong(buffer));
            }
            else
            {
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "timestamp"));
            }
        }

        public static Guid ReadUuid(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
            }

            if (formatCode == FormatCode.Uuid)
            {
                return AmqpBitConverter.ReadUuid(buffer);
            }
            else
            {
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "uuid"));
            }
        }

        public static byte[] ReadBinary(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
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
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "binary"));
            }

            buffer.Validate(false, count);
            byte[] value = new byte[count];
            Array.Copy(buffer.Buffer, buffer.Offset, value, 0, count);
            buffer.Complete(count);

            return value;
        }

        public static string ReadString(ByteBuffer buffer, byte formatCode)
        {
            return ReadString(buffer, formatCode, FormatCode.String8Utf8, FormatCode.String32Utf8, "string");
        }

        public static string ReadSymbol(ByteBuffer buffer, byte formatCode)
        {
            return ReadString(buffer, formatCode, FormatCode.Symbol8, FormatCode.Symbol32, "symbol");
        }

        public static object[] ReadList(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
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
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "list"));
            }

            object[] value = new object[count];
            for (int i = 0; i < count; ++i)
            {
                value[i] = ReadObject(buffer);
            }

            return value;
        }

        public static object[] ReadArray(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
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
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "list"));
            }

            formatCode = Encoder.ReadFormatCode(buffer);
            Decode decode = (Decode)decoders[formatCode];
            if (decode == null)
            {
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "*"));
            }

            object[] value = new object[count];
            for (int i = 0; i < count; ++i)
            {
                value[i] = decode(buffer, formatCode);
            }

            return value;
        }

        public static Map ReadMap(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
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
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "map"));
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

        static string ReadString(ByteBuffer buffer, byte formatCode, byte code8, byte code32, string type)
        {
            if (formatCode == FormatCode.Unknown)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
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
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, type));
            }

            buffer.Validate(false, count);
            string value = new string(Encoding.UTF8.GetChars(buffer.Buffer, buffer.Offset, count));
            buffer.Complete(count);

            return value;
        }
    }
}
