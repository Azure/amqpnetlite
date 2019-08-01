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
    /// The state of a transactional message transfer.
    /// </summary>
    public sealed class TransactionalState : DeliveryState
    {
        /// <summary>
        /// Initializes a transactional state object.
        /// </summary>
        public TransactionalState()
            : base(Codec.TransactionalState, 2)
        {
        }

        private byte[] txnId;
        /// <summary>
        /// Gets or sets the txn-id field.
        /// </summary>
        public byte[] TxnId
        {
            get { return this.txnId; }
            set { this.txnId = value; }
        }

        private Outcome outcome;
        /// <summary>
        /// Gets or sets the outcome field.
        /// </summary>
        public Outcome Outcome
        {
            get { return this.outcome; }
            set { this.outcome = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.txnId = Encoder.ReadBinary(buffer);
            }

            if (count-- > 0)
            {
                this.outcome = (Outcome)Encoder.ReadObject(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteBinary(buffer, txnId, true);
            Encoder.WriteObject(buffer, outcome, true);
        }

#if TRACE
        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.GetDebugString(
                "txn-state",
                new object[] { "txn-id", "outcome" },
                new object[] { txnId, outcome });
        }
#endif
    }
}