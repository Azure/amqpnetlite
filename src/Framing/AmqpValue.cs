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

    public sealed class AmqpValue : RestrictedDescribed
    {
        object value;

        public AmqpValue()
            : base(Codec.AmqpValue)
        {
        }

#if DOTNET
        public object Value
        {
            get { return this.GetValue<object>(); }
            set { this.value = value; }
        }

        internal T GetValue<T>()
        {
            ByteBuffer buffer = this.value as ByteBuffer;
            if (buffer != null)
            {
                if (typeof(T) == typeof(object))
                {
                    return (T)Encoder.ReadObject(buffer);
                }
                else
                {
                    return Serialization.AmqpSerializer.Deserialize<T>(buffer);
                }
            }

            return (T)this.value;
        }

        internal override void EncodeValue(ByteBuffer buffer)
        {
            Serialization.AmqpSerializer.Serialize(buffer, this.value);
        }

        internal override void DecodeValue(ByteBuffer buffer)
        {
            int offset = buffer.Offset;
            byte formatCode = Encoder.ReadFormatCode(buffer);
            if (formatCode == FormatCode.Described)
            {
                // descriptor
                Encoder.ReadObject(buffer);

                int typeCode = Encoder.ReadFormatCode(buffer) & 0xF0;
                if (typeCode == 0xC0 || typeCode == 0xD0)
                {
                    int size = (int)AmqpBitConverter.ReadUInt(buffer);
                    buffer.Complete(size);
                    int count = buffer.Offset - offset;
                    this.value = new ByteBuffer(buffer.Buffer, offset, count, count);
                    return;
                }
            }

            buffer.Seek(offset);
            this.value = Encoder.ReadObject(buffer);
        }
#else
        public object Value
        {
            get { return this.value; }
            set { this.value = value; }
        }

        internal override void EncodeValue(ByteBuffer buffer)
        {
            Encoder.WriteObject(buffer, this.Value);
        }

        internal override void DecodeValue(ByteBuffer buffer)
        {
            this.Value = Encoder.ReadObject(buffer);
        }
#endif
    }
}