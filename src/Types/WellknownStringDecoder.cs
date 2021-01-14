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
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Implements a simple IStringDecoder that caches well-known strings.
    /// </summary>
    /// <remarks>
    /// If the encoded bytes of a buffer match the encoded value of a known string,
    /// existing known string instance is returned, avoiding any allocations.
    /// Note that this class is not thread-safe, so all calls to AddWellknownString
    /// should be made before the StringDecoder is passed to Encoder.StringDecoder.
    /// </remarks>
    public class WellknownStringDecoder : IStringDecoder
    {
        readonly Dictionary<BufferKey, string> knownStrings = new Dictionary<BufferKey, string>();

        /// <summary>
        /// Adds a known string. Must be called before it is set to Encoder.StringDecoder.
        /// </summary>
        /// <param name="knownString">The known string instance</param>
        public void AddWellknownString(string knownString)
        {
            if (knownString == null)
            {
                throw new ArgumentNullException("knownString");
            }

            byte[] encodedString = Encoding.UTF8.GetBytes(knownString);
            var key = new BufferKey(encodedString);
            this.knownStrings[key] = knownString;
        }

        /// <summary>
        /// Performs a lookup based on the contents of the passed byte buffer. If the byte contents match a known string, the cached string instance is returned.
        /// Otherwise a new string is created by decoding the buffer contents.
        /// </summary>
        /// <param name="buffer">The byte array segment to read from</param>
        /// <returns>A string instance that match the decoded value of the passed byte buffer.</returns>
        public string DecodeString(ArraySegment<byte> buffer)
        {
            var searchKey = new BufferKey(buffer);
            string knownString;
            if (this.knownStrings.TryGetValue(searchKey, out knownString))
                return knownString;

            return Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
        }

        private struct BufferKey : IEquatable<BufferKey>
        {
            readonly ArraySegment<byte> encodedString;

            public BufferKey(byte[] encodedString)
            {
                this.encodedString = new ArraySegment<byte>(encodedString);
            }

            public BufferKey(ArraySegment<byte> encodedString)
            {
                this.encodedString = encodedString;
            }

            public bool Equals(BufferKey other)
            {
                if (this.encodedString.Count != other.encodedString.Count)
                    return false;

                for (int i = 0; i < this.encodedString.Count; i++)
                {
                    if (this.encodedString.Array[this.encodedString.Offset + i] != other.encodedString.Array[other.encodedString.Offset + i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is BufferKey))
                {
                    return false;
                }

                return Equals((BufferKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    int endOffset = this.encodedString.Offset + this.encodedString.Count;
                    for (var i = this.encodedString.Offset; i < endOffset; i++)
                    {
                        hash = hash * 31 + this.encodedString.Array[i];
                    }

                    return hash;
                }
            }
        }
    }
}