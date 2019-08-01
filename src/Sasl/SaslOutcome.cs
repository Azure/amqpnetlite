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
    /// Indicates the outcome of the sasl dialog.
    /// </summary>
    public class SaslOutcome : DescribedList
    {
        /// <summary>
        /// Initializes a SaslOutcome object.
        /// </summary>
        public SaslOutcome()
            : base(Codec.SaslOutcome, 2)
        {
        }

        private SaslCode code;
        /// <summary>
        /// Gets or sets the outcome of the sasl dialog.
        /// </summary>
        public SaslCode Code
        {
            get { return this.code; }
            set { this.code = value; }
        }

        private byte[] additionalData;
        /// <summary>
        /// Gets or sets the additional data as specified in RFC-4422.
        /// </summary>
        public byte[] AdditionalData
        {
            get { return this.additionalData; }
            set { this.additionalData = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.code = (SaslCode)Encoder.ReadUByte(buffer);
            }

            if (count-- > 0)
            {
                this.additionalData = Encoder.ReadBinary(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteUByte(buffer, (byte?)code);
            Encoder.WriteBinary(buffer, additionalData, true);
        }

#if TRACE
        /// <summary>
        /// Returns a string that represents the current SASL outcome object.
        /// </summary>
        public override string ToString()
        {
            return this.GetDebugString(
                "sasl-outcome",
                new object[] { "code", "additional-data" },
                new object[] { code, additionalData });
        }
#endif
    }
}