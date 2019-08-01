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

namespace Amqp.Sasl
{
    using Amqp.Framing;
    using Amqp.Types;

    /// <summary>
    /// Security mechanism response.
    /// </summary>
    public class SaslResponse : DescribedList
    {
        /// <summary>
        /// Initializes a SaslResponse object.
        /// </summary>
        public SaslResponse()
            : base(Codec.SaslResponse, 1)
        {
        }

        private byte[] response;
        /// <summary>
        /// Gets or sets the security response data.
        /// </summary>
        public byte[] Response
        {
            get { return this.response; }
            set { this.response = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.response = Encoder.ReadBinary(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteBinary(buffer, response, true);
        }

#if TRACE
        /// <summary>
        /// Returns a string that represents the current SASL response object.
        /// </summary>
        public override string ToString()
        {
            return this.GetDebugString(
                "sasl-response",
                new object[] { "response" },
                new object[] { response });
        }
#endif
    }
}