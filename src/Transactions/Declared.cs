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

namespace Amqp.Transactions
{
    using Amqp.Framing;
    using Amqp.Types;

    /// <summary>
    /// Indicates that a transaction identifier has successfully been allocated.
    /// </summary>
    public sealed class Declared : Outcome
    {
        byte[] txnId;

        /// <summary>
        /// Initializes a declared object.
        /// </summary>
        public Declared()
            : base(Codec.Declared, 1)
        {
        }

        /// <summary>
        /// Gets or sets the txn-id field.
        /// </summary>
        public byte[] TxnId
        {
            get { return this.GetField(0, this.txnId); }
            set { this.SetField(0, ref this.txnId, value); }
        }

        internal override void WriteField(ByteBuffer buffer, int index)
        {
            switch (index)
            {
                case 0:
                    Encoder.WriteBinary(buffer, this.txnId, true);
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
                    this.txnId = Encoder.ReadBinary(buffer, formatCode);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
        }
        
#if TRACE
        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.GetDebugString(
                "declared",
                new object[] { "txn-id" },
                new object[] { txnId });
        }
#endif
    }
}