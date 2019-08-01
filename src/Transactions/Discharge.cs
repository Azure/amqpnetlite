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
    /// Message body for discharging a transaction.
    /// </summary>
    public sealed class Discharge : DescribedList
    {
        /// <summary>
        /// Initializes a discharge object.
        /// </summary>
        public Discharge()
            : base(Codec.Discharge, 2)
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

        private bool? fail;
        /// <summary>
        /// Gets or sets the fail field.
        /// </summary>
        public bool Fail
        {
            get { return this.fail == null ? false : this.fail.Value; }
            set { this.fail = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.txnId = Encoder.ReadBinary(buffer);
            }

            if (count-- > 0)
            {
                this.fail = Encoder.ReadBoolean(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteBinary(buffer, txnId, true);
            Encoder.WriteBoolean(buffer, fail, true);
        }

#if TRACE
        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.GetDebugString(
                "discharge",
                new object[] { "txn-id", "fail" },
                new object[] { txnId, fail });
        }
#endif
    }
}