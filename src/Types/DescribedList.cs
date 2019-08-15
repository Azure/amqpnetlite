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
    using Amqp.Framing;
    using Amqp.Sasl;

    /// <summary>
    /// The DescribedList class consist of a descriptor and an AMQP list.
    /// </summary>
    public abstract class DescribedList : RestrictedDescribed
    {
        readonly int fieldCount;
        int fields; // bitmask that stores an 'is null' flag for each field

        /// <summary>
        /// Initializes the described list object.
        /// </summary>
        /// <param name="descriptor">The descriptor of the concrete described list class.</param>
        /// <param name="fieldCount">The number of fields of the concrete described list class.</param>
        protected DescribedList(Descriptor descriptor, int fieldCount)
            : base(descriptor)
        {
            if (fieldCount > 32)
            {
                throw new NotSupportedException("Maximum number of fields allowed is 32.");
            }

            this.fieldCount = fieldCount;
        }
        
        /// <summary>
        /// Examines the list to check if a field is set.
        /// </summary>
        /// <param name="index">Zero-based offset of the field in the list.</param>
        /// <returns>True if a value is set; otherwise false.</returns>
        /// <remarks>The field index can be found in the description of each field.</remarks>
        public bool HasField(int index)
        {
            this.CheckFieldIndex(index);
            return (this.fields & (1 << index)) > 0;
        }

        /// <summary>
        /// Resets the value of a field to null.
        /// </summary>
        /// <param name="index">Zero-based offset of the field in the list.</param>
        /// <remarks>The field index can be found in the description of each field.</remarks>
        public void ResetField(int index)
        {
            this.CheckFieldIndex(index);
            this.fields &= ~(1 << index);
        }

#if NETMF
        #region non-generic NETMF setter / getter
        internal void SetField(int index, ref uint field, uint value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref ushort field, ushort value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref bool field, bool value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref ulong field, ulong value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref byte field, byte value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref string field, string value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref byte[] field, byte[] value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref Outcome field, Outcome value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref Map field, Map value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref ReceiverSettleMode field, ReceiverSettleMode value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref SenderSettleMode field, SenderSettleMode value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref DeliveryState field, DeliveryState value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref Error field, Error value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref Fields field, Fields value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref DateTime field, DateTime value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref Symbol field, Symbol value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref SaslCode field, SaslCode value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal void SetField(int index, ref object field, object value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal uint GetField(int index, uint field, uint defaultValue = default(uint))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal ushort GetField(int index, ushort field, ushort defaultValue = default(ushort))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal bool GetField(int index, bool field, bool defaultValue = default(bool))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal byte GetField(int index, byte field, byte defaultValue = default(byte))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal ulong GetField(int index, ulong field, ulong defaultValue = default(ulong))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal string GetField(int index, string field, string defaultValue = default(string))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal byte[] GetField(int index, byte[] field, byte[] defaultValue = default(byte[]))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal Outcome GetField(int index, Outcome field, Outcome defaultValue = default(Outcome))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal Fields GetField(int index, Fields field, Fields defaultValue = default(Fields))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal Map GetField(int index, Map field, Map defaultValue = default(Map))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal ReceiverSettleMode GetField(int index, ReceiverSettleMode field, ReceiverSettleMode defaultValue = default(ReceiverSettleMode))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal SenderSettleMode GetField(int index, SenderSettleMode field, SenderSettleMode defaultValue = default(SenderSettleMode))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal DeliveryState GetField(int index, DeliveryState field, DeliveryState defaultValue = default(DeliveryState))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal Error GetField(int index, Error field, Error defaultValue = default(Error))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal DateTime GetField(int index, DateTime field, DateTime defaultValue = default(DateTime))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal Symbol GetField(int index, Symbol field, Symbol defaultValue = default(Symbol))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal SaslCode GetField(int index, SaslCode field, SaslCode defaultValue = default(SaslCode))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        internal object GetField(int index, object field, object defaultValue = default(object))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }

        #endregion
#else
        internal void SetField<T>(int index, ref T field, T value)
        {
            this.fields |= (1 << index);
            field = value;
        }

        internal T GetField<T>(int index, T field, T defaultValue = default(T))
        {
            if ((this.fields & (1 << index)) == 0)
            {
                return defaultValue;
            }

            return field;
        }
#endif

        internal override void DecodeValue(ByteBuffer buffer)
        {
            byte formatCode = Encoder.ReadFormatCode(buffer);
            int size;
            int count;
            Encoder.ReadListCount(buffer, formatCode, out size, out count);
            for (int i = 0; i < count; i++)
            {
                formatCode = Encoder.ReadFormatCode(buffer);
                if (formatCode != FormatCode.Null)
                {
                    this.ReadField(buffer, i, formatCode);
                    this.fields |= (1 << i);
                }
            }
        }

        internal abstract void WriteField(ByteBuffer buffer, int index);

        internal abstract void ReadField(ByteBuffer buffer, int index, byte formatCode);
        

        internal override void EncodeValue(ByteBuffer buffer)
        {
            if (this.fields == 0)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.List0);
            }
            else
            {
                // Count non-null fields by removing leading zeros
                int count = 0;
                int temp = this.fields;
                while (temp > 0)
                {
                    count++;
                    temp <<= 1;
                }

                count = 32 - count;
                int pos = buffer.WritePos;
                AmqpBitConverter.WriteUByte(buffer, 0);
                AmqpBitConverter.WriteULong(buffer, 0);
                for (int i = 0; i < count; i++)
                {
                    if ((this.fields & (1 << i)) > 0)
                    {
                        this.WriteField(buffer, i);
                    }
                    else
                    {
                        AmqpBitConverter.WriteUByte(buffer, FormatCode.Null);
                    }
                }

                Encoder.WriteListCount(buffer, pos, count, true);
            }
        }
        
#if TRACE
        /// <summary>
        /// Returns a string representing the current object for tracing purpose.
        /// </summary>
        /// <param name="name">The object name.</param>
        /// <param name="fieldNames">The field names.</param>
        /// <param name="fieldValues">The field values.</param>
        /// <returns></returns>
        protected string GetDebugString(string name, object[] fieldNames, object[] fieldValues)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(name);
            sb.Append('(');
            bool addComma = false;
            for (int i = 0; i < fieldValues.Length; i++)
            {
                if (fieldValues[i] != null)
                {
                    if (addComma)
                    {
                        sb.Append(",");
                    }

                    sb.Append(fieldNames[i]);
                    sb.Append(":");
                    sb.Append(Trace.GetTraceObject(fieldValues[i]));
                    addComma = true;
                }
            }

            sb.Append(')');

            return sb.ToString();
        }
#endif

        void CheckFieldIndex(int index)
        {
            if (index < 0 || index >= this.fieldCount)
            {
                throw new ArgumentOutOfRangeException(Fx.Format("Field index is invalid. {0} has {1} fields.",
                    this.GetType().Name, this.fieldCount));
            }
        }
    }
}
