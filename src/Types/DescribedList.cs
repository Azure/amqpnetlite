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

    /// <summary>
    /// The DescribedList class consist of a descriptor and an AMQP list.
    /// </summary>
    public abstract class DescribedList : RestrictedDescribed
    {
        private readonly int fieldCount;

        /// <summary>
        /// Initializes the described list object.
        /// </summary>
        /// <param name="descriptor">The descriptor of the concrete described list class.</param>
        /// <param name="fieldCount">The number of fields of the concrete described list class.</param>
        protected DescribedList(Descriptor descriptor, int fieldCount)
            : base(descriptor)
        {
            this.fieldCount = fieldCount;
        }
        
        internal override void DecodeValue(ByteBuffer buffer)
        {
            byte formatCode = Encoder.ReadFormatCode(buffer);
            if (formatCode == FormatCode.Null)
            {
                return;
            }

            int size;
            int count;
            if (formatCode == FormatCode.List0)
            {
                size = count = 0;
                return;
            }

            if (formatCode == FormatCode.List8)
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
                throw Encoder.InvalidFormatCodeException(formatCode, buffer.Offset);
            }

            OnDecode(buffer, count);
        }

        /// <summary>
        /// Examines the list to check if a field is set.
        /// </summary>
        /// <param name="index">Zero-based offset of the field in the list.</param>
        /// <returns>True if a value is set; otherwise false.</returns>
        public bool HasField(int index)
        {
            this.CheckFieldIndex(index);
            return this.fields[index] != null;
        }

        /// <summary>
        /// Resets the value of a field to null.
        /// </summary>
        /// <param name="index">Zero-based offset of the field in the list.</param>
        public void ResetField(int index)
        {
            this.CheckFieldIndex(index);
            this.fields[index] = null;
        }

        /// <summary>
        /// Decodes all properties from a ByteBuffer
        /// </summary>
        /// <param name="buffer">the ByteBuffer to read from</param>
        /// <param name="count">the number of fields that are available for read</param>
        internal virtual void OnDecode(ByteBuffer buffer, int count)
        {
        }

        /// <summary>
        /// Encodes all properties into a ByteBuffer
        /// </summary>
        /// <param name="buffer">the ByteBuffer to write to</param>
        internal virtual void OnEncode(ByteBuffer buffer)
        {
        }

        internal override void EncodeValue(ByteBuffer buffer)
        {
            int pos = buffer.WritePos;
            AmqpBitConverter.WriteUByte(buffer, 0);
            AmqpBitConverter.WriteUInt(buffer, 0);
            AmqpBitConverter.WriteUInt(buffer, 0);

            OnEncode(buffer);

            int size = buffer.WritePos - pos - 9;

            if (size < byte.MaxValue && this.fieldCount <= byte.MaxValue)
            {
                buffer.Buffer[pos] = FormatCode.List8;
                buffer.Buffer[pos + 1] = (byte)(size + 1);
                buffer.Buffer[pos + 2] = (byte)this.fieldCount;
                Array.Copy(buffer.Buffer, pos + 9, buffer.Buffer, pos + 3, size);
                buffer.Shrink(6);
            }
            else
            {
                buffer.Buffer[pos] = FormatCode.List32;
                AmqpBitConverter.WriteInt(buffer.Buffer, pos + 1, size + 4);
                AmqpBitConverter.WriteInt(buffer.Buffer, pos + 5, this.fieldCount);
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
            if (index < 0 || index >= this.fields.Length)
            {
                throw new ArgumentOutOfRangeException(Fx.Format("Field index is invalid. {0} has {1} fields.",
                    this.GetType().Name, this.fields.Length));
            }
        }
    }
}
