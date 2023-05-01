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
    /// Represents one or more <see cref="Data"/> sections, typically used as a message body.
    /// </summary>
    /// <remarks>As a message body, the list is encoded as continuous Data sections without
    /// the list encoding preamble (format code, size and count). If the list is empty, it is
    /// equivalent to one Data section with empty binary data.</remarks>
    public sealed class DataList : RestrictedDescribed
    {
        Data[] array;
        int count;

        /// <summary>
        /// Initializes a Data object.
        /// </summary>
        public DataList()
            : base(Codec.Data)
        {
        }

        /// <summary>
        /// Gets the number of elements.
        /// </summary>
        public int Count
        {
            get { return this.count; }
        }

        /// <summary>
        /// Gets the <see cref="Data"/> element at index. Caller should check <see cref="Count"/>
        /// before calling this method to ensure that index is valid.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <returns>The <see cref="Data"/> element at index.</returns>
        public Data this[int index]
        {
            get { return this.array[index]; }
        }

        /// <summary>
        /// Adds a <see cref="Data"/> section.
        /// </summary>
        /// <param name="data"></param>
        public void Add(Data data)
        {
            if (this.array == null)
            {
                this.array = new Data[4];
            }
            else if (this.count == this.array.Length)
            {
                var temp = new Data[this.count * 2];
                Array.Copy(this.array, temp, this.count);
                this.array = temp;
            }

            this.array[this.count++] = data;
        }

        /// <inheritdoc cref="Object.GetHashCode()" />
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <inheritdoc cref="Object.Equals(object)" />
        public override bool Equals(object obj)
        {
            var data = obj as Data;
            if (data != null)
            {
                if (data.Length == 0 && this.count == 0)
                {
                    return true;
                }
            }

            return base.Equals(obj);
        }

        internal Data[] ToArray()
        {
            if (this.array.Length == this.count)
            {
                return this.array;
            }

            var copy = new Data[this.count];
            Array.Copy(this.array, copy, this.count);
            return copy;
        }

        internal override void EncodeValue(ByteBuffer buffer)
        {
            if (this.count == 0)
            {
                // Encode this as an empty binary.
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Binary8);
                AmqpBitConverter.WriteUByte(buffer, 0);
            }
            else
            {
                this.array[0].EncodeValue(buffer);
                for (int i = 1; i < this.count; i++)
                {
                    this.array[i].Encode(buffer);
                }
            }
        }

        internal override void DecodeValue(ByteBuffer buffer)
        {
            // Should never be called directly.
            throw new InvalidOperationException();
        }
    }
}