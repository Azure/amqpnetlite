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

    /// <summary>
    /// An amqp-value section contains a single AMQP value.
    /// </summary>
    public sealed class AmqpValue : RestrictedDescribed
    {
        object value;

        /// <summary>
        /// Initializes an AmqpValue object.
        /// </summary>
        public AmqpValue()
            : base(Codec.AmqpValue)
        {
        }

#if (DOTNET || DOTNET35)
        ByteBuffer valueBuffer;
        bool valueDecoded;
        byte binaryOffset;

        /// <summary>
        /// Gets or sets the value in the body section.
        /// </summary>
        public object Value
        {
            get
            {
                if (!this.valueDecoded)
                {
                    this.value = Encoder.ReadObject(this.valueBuffer);
                    this.valueDecoded = true;
                }

                return this.value;
            }
            set
            {
                this.value = value;
                this.valueDecoded = true;
            }
        }

        internal T GetValue<T>()
        {
            if (this.value != null && typeof(T).IsAssignableFrom(this.value.GetType()))
            {
                return (T)this.value;
            }
            else if (typeof(T) == typeof(ByteBuffer))
            {
                if (this.binaryOffset == 0)
                {
                    throw new ArgumentException("Body is not binary type.");
                }

                var payload = new ByteBuffer(this.valueBuffer.Buffer, this.valueBuffer.Offset + this.binaryOffset,
                    this.valueBuffer.Length - this.binaryOffset, this.valueBuffer.Length - this.binaryOffset);
                return (T)(object)payload;
            }
            else if (typeof(T) == typeof(byte[]))
            {
                if (this.binaryOffset == 0)
                {
                    throw new ArgumentException("Body is not binary type.");
                }

                byte[] payload = new byte[this.valueBuffer.Length - this.binaryOffset];
                Buffer.BlockCopy(this.valueBuffer.Buffer, this.valueBuffer.Offset + this.binaryOffset, payload, 0, payload.Length);
                return (T)(object)payload;
            }
            else
            {
                return Serialization.AmqpSerializer.Deserialize<T>(this.valueBuffer);
            }
        }

        internal override void EncodeValue(ByteBuffer buffer)
        {
            var byteBuffer = this.value as ByteBuffer;
            if (byteBuffer != null)
            {
                Encoder.WriteBinaryBuffer(buffer, byteBuffer);
            }
            else
            {
                Serialization.AmqpSerializer.Serialize(buffer, this.value);
            }
        }

        internal override void DecodeValue(ByteBuffer buffer)
        {
            int offset = buffer.Offset;
            byte formatCode = Encoder.ReadFormatCode(buffer);
            if (formatCode == FormatCode.Binary8)
            {
                this.binaryOffset = 2;
            }
            else if (formatCode == FormatCode.Binary32)
            {
                this.binaryOffset = 5;
            }
            else
            {
                while (formatCode == FormatCode.Described)
                {
                    Encoder.ReadObject(buffer);
                    formatCode = Encoder.ReadFormatCode(buffer);
                }
            }

            int count;
            if (formatCode >= 0xA0)
            {
                //bin8  a0 10100001
                //str8  a1 10100001
                //sym8  a3 10100011
                //list8 c0 11000000
                //map8  c1 11000001
                //arr8  e0 11100000
                //bin32 b0 10110000
                //str32 b1 10110001
                //sym32 b3 10110011
                //lit32 d0 11010000
                //map32 d1 11010001
                //arr32 f0 11110000
                if ((formatCode & 0x10) == 0)
                {
                    count = AmqpBitConverter.ReadUByte(buffer);
                }
                else
                {
                    count = AmqpBitConverter.ReadInt(buffer);
                }
            }
            else
            {
                count = (1 << ((formatCode >> 4) - 4)) >> 1;
            }

            buffer.Validate(false, count);
            buffer.Complete(count);

            int size = buffer.Offset - offset;
            this.valueBuffer = new ByteBuffer(buffer.Buffer, offset, size, size);
        }
#else
        public object Value
        {
            get { return this.value; }
            set { this.value = value; }
        }

        internal override void EncodeValue(ByteBuffer buffer)
        {
            Encoder.WriteObject(buffer, this.value);
        }

        internal override void DecodeValue(ByteBuffer buffer)
        {
            this.value = Encoder.ReadObject(buffer);
        }
#endif
    }
}