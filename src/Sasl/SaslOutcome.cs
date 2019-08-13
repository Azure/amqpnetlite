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
        SaslCode code;
        byte[] additionalData;

        /// <summary>
        /// Initializes a SaslOutcome object.
        /// </summary>
        public SaslOutcome()
            : base(Codec.SaslOutcome, 2)
        {
        }

        /// <summary>
        /// Gets or sets the outcome of the sasl dialog (index=0).
        /// </summary>
        public SaslCode Code
        {
            get { return this.GetField(0, this.code); }
            set { this.SetField(0, ref this.code, value); }
        }

        /// <summary>
        /// Gets or sets the additional data as specified in RFC-4422 (index=1).
        /// </summary>
        public byte[] AdditionalData
        {
            get { return this.GetField(1, this.additionalData); }
            set { this.SetField(1, ref this.additionalData, value); }
        }

        internal override void WriteField(ByteBuffer buffer, int index)
        {
            switch (index)
            {
                case 0:
                    Encoder.WriteUByte(buffer, (byte)this.code);
                    break;
                case 1:
                    Encoder.WriteBinary(buffer, this.additionalData, true);
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
                    this.code = (SaslCode)Encoder.ReadUByte(buffer, formatCode);
                    break;
                case 1:
                    this.additionalData = Encoder.ReadBinary(buffer, formatCode);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
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