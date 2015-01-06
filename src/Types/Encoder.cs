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

    delegate void Encode(ByteBuffer buffer, object value);

    delegate object Decode(ByteBuffer buffer, byte formatCode);

    static class Encoder
    {
        class Serializer
        {
            public Encode Encoder;
            public Decode Decoder;
        }

        public static readonly DateTime StartOfEpoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static readonly Serializer[] serializers;
        static readonly Map codecByType;
        static readonly Map codecByFormatCode;
        static Map knownDescrided;

        static Encoder()
        {
            serializers = new Serializer[]
            {
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { AmqpBitConverter.WriteUByte(b, FormatCode.Null); },
                    Decoder = delegate(ByteBuffer b, byte c) { return null; }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteBoolean(b, (bool)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadBoolean(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteUByte(b, (byte)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadUByte(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteUShort(b, (ushort)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadUShort(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteUInt(b, (uint)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadUInt(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteULong(b, (ulong)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadULong(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteByte(b, (sbyte)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadByte(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteShort(b, (short)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadShort(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteInt(b, (int)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadInt(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteLong(b, (long)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadLong(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteFloat(b, (float)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadFloat(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteDouble(b, (double)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadDouble(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteChar(b, (char)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadChar(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteTimestamp(b, (DateTime)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadTimestamp(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteUuid(b, (Guid)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadUuid(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteBinary(b, (byte[])o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadBinary(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteString(b, (string)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadString(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteSymbol(b, (Symbol)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadSymbol(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteList(b, (IList)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadList(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteMap(b, (Map)o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadMap(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteArray(b, (object[])o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadArray(b, c); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { ((Multiple)o).Encode(b); },
                    Decoder = delegate(ByteBuffer b, byte c) { return Multiple.From(ReadObject(b)); }
                },
                new Serializer()
                {
                    Encoder = delegate(ByteBuffer b, object o) { WriteObject(b, o); },
                    Decoder = delegate(ByteBuffer b, byte c) { return ReadObject(b); }
                },
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
                { typeof(object[]), serializers[20] },
                { typeof(Multiple), serializers[21] },
                { typeof(object),   serializers[22] },
            };

            codecByFormatCode = new Map()
            {
                { FormatCode.Null,          serializers[0] },
                { FormatCode.Boolean,       serializers[1] },
                { FormatCode.BooleanTrue,   serializers[1] },
                { FormatCode.BooleanFalse,  serializers[1] },
                { FormatCode.UByte,         serializers[2] },
                { FormatCode.UShort,        serializers[3] },
                { FormatCode.UInt0,         serializers[4] },
                { FormatCode.SmallUInt,     serializers[4] },
                { FormatCode.UInt,          serializers[4] },
                { FormatCode.ULong0,        serializers[5] },
                { FormatCode.SmallULong,    serializers[5] },
                { FormatCode.ULong,         serializers[5] },
                { FormatCode.Byte,          serializers[6] },
                { FormatCode.Short,         serializers[7] },
                { FormatCode.Int,           serializers[8] },
                { FormatCode.SmallInt,      serializers[8] },
                { FormatCode.Long,          serializers[9] },
                { FormatCode.SmallLong,     serializers[9] },
                { FormatCode.Float,         serializers[10] },
                { FormatCode.Double,        serializers[11] },
                { FormatCode.Char,          serializers[12] },
                { FormatCode.TimeStamp,     serializers[13] },
                { FormatCode.Uuid,          serializers[14] },
                { FormatCode.Binary8,       serializers[15] },
                { FormatCode.Binary32,      serializers[15] },
                { FormatCode.String8Utf8,   serializers[16] },
                { FormatCode.String32Utf8,  serializers[16] },
                { FormatCode.Symbol8,       serializers[17] },
                { FormatCode.Symbol32,      serializers[17] },
                { FormatCode.List0,         serializers[18] },
                { FormatCode.List8,         serializers[18] },
                { FormatCode.List32,        serializers[18] },
                { FormatCode.Map8,          serializers[19] },
                { FormatCode.Map32,         serializers[19] },
                { FormatCode.Array8,        serializers[20] },
                { FormatCode.Array32,       serializers[20] },
            };
        }

        public static bool TryGetCodec(Type type, out Encode encoder, out Decode decoder)
        {
            Serializer codec = (Serializer)codecByType[type];
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
                Serializer serializer = (Serializer)codecByType[value.GetType()];
                if (serializer != null)
                {
                    serializer.Encoder(buffer, value);
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
                for (int i = 1; i < value.Count; ++i)
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
            Serializer serializer = (Serializer)codecByFormatCode[formatCode];
            if (serializer != null)
            {
                return serializer.Decoder(buffer, formatCode);
            }
            else if (formatCode == FormatCode.Described)
            {
                Described described;
                object descriptor = Encoder.ReadObject(buffer);
                CreateDescribed create = null;
                if (knownDescrided == null || (create = (CreateDescribed)knownDescrided[descriptor]) == null)
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

        public static Symbol ReadSymbol(ByteBuffer buffer, byte formatCode)
        {
            return ReadString(buffer, formatCode, FormatCode.Symbol8, FormatCode.Symbol32, "symbol");
        }

        public static List ReadList(ByteBuffer buffer, byte formatCode)
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

            List value = new List();
            for (int i = 0; i < count; ++i)
            {
                value.Add(ReadObject(buffer));
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
            Serializer codec = (Serializer)codecByFormatCode[formatCode];
            if (codec == null)
            {
                throw new AmqpException(ErrorCode.DecodeError,
                    Fx.Format(SRAmqp.AmqpInvalidFormatCode, formatCode, buffer.Offset, "*"));
            }

            object[] value = new object[count];
            for (int i = 0; i < count; ++i)
            {
                value[i] = codec.Decoder(buffer, formatCode);
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
