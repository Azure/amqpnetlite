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
    /// Contains the security mechanism challenge data defined by the SASL specification.
    /// </summary>
    public class SaslChallenge : DescribedList
    {
        /// <summary>
        /// Initializes the SASL challenge object.
        /// </summary>
        public SaslChallenge()
            : base(Codec.SaslChallenge, 1)
        {
        }

        private byte[] challenge;
        /// <summary>
        /// Gets or sets the security challenge data.
        /// </summary>
        public byte[] Challenge
        {
            get { return this.challenge; }
            set { this.challenge = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.challenge = Encoder.ReadBinary(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteBinary(buffer, challenge, true);
        }

#if TRACE
        /// <summary>
        /// Returns a string that represents the current SASL challenge object.
        /// </summary>
        public override string ToString()
        {
            return this.GetDebugString(
                "sasl-challenge",
                new object[] { "challenge" },
                new object[] { challenge });
        }
#endif
    }
}