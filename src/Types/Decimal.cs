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
    using System.Text;

    /// <summary>
    /// Defines the AMQP decimal types. It does not support decimal arithmetic operations.
    /// It is defined as a wrapper of the decimal payload for protocol passthrough.
    /// </summary>
    public class Decimal
    {
        byte[] bytes;

        /// <summary>
        /// Initializes a new Decimal object to wrap the bytes.
        /// </summary>
        /// <param name="bytes">AMQP serialized decimal value (decimal32/64/128)</param>
        public Decimal(byte[] bytes)
        {
            if (bytes.Length != 4 && bytes.Length != 8 && bytes.Length != 16)
            {
                throw new ArgumentException("decimal payload must be 4, 8, or 16 bytes");
            }

            this.bytes = bytes;
        }

        /// <summary>
        /// Gets the serialized bytes of the decimal value.
        /// </summary>
        public byte[] Bytes
        {
            get { return this.bytes; }
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>A hash code for the current Decimal object.</returns>
        public override int GetHashCode()
        {
            int hash = 17;
            for (int i = 0; i < this.bytes.Length; i++)
            {
                hash = hash * 31 + this.bytes[i].GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// Determines whether the specified Decimal is equal to the current Decimal.
        /// </summary>
        /// <param name="obj">The object to compare with the current value.</param>
        /// <returns>true if the specified object is equal to the current value; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            Decimal other = obj as Decimal;
            if (other == null || other.bytes.Length != this.bytes.Length)
            {
                return false;
            }

            for (int i = 0; i < this.bytes.Length; i++)
            {
                if (this.bytes[i] != other.bytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns a string that represents the current Decimal.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(this.bytes.Length * 4 + 8);
            sb.Append("decimal").Append('(');
            for (int i = 0; i < this.bytes.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append('.');
                }

                sb.Append(this.bytes[i]);
            }

            sb.Append(')');

            return sb.ToString();
        }
    }
}
