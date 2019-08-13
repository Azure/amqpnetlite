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
        byte[] challenge;

        /// <summary>
        /// Initializes the SASL challenge object.
        /// </summary>
        public SaslChallenge()
            : base(Codec.SaslChallenge, 1)
        {
        }

        /// <summary>
        /// Gets or sets the security challenge data (index=0).
        /// </summary>
        public byte[] Challenge
        {
            get { return this.GetField(0, this.challenge); }
            set { this.SetField(0, ref this.challenge, value); }
        }

        internal override void WriteField(ByteBuffer buffer, int index)
        {
            switch (index)
            {
                case 0:
                    Encoder.WriteBinary(buffer, this.challenge, true);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
        }

        internal override void ReadField(ByteBuffer buffer, int index, byte formatCode)
        {
            switch (index)
            {
                case 0:
                    this.challenge = Encoder.ReadBinary(buffer, formatCode);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
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