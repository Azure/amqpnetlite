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

namespace Amqp.Framing
{
    using Amqp.Types;

    /// <summary>
    /// The Transfer class defines an transfer frame to send messages across a link. 
    /// </summary>
    public sealed class Transfer : DescribedList
    {
        /// <summary>
        /// Initializes an transfer object.
        /// </summary>
        public Transfer()
            : base(Codec.Transfer, 11)
        {
        }

        /// <summary>
        /// Gets or sets the delivery-id field.
        /// </summary>
        public bool HasDeliveryId
        {
            get { return this.Fields[1] != null; }
        }

        /// <summary>
        /// Gets or sets the handle field.
        /// </summary>
        public uint Handle
        {
            get { return this.Fields[0] == null ? uint.MaxValue : (uint)this.Fields[0]; }
            set { this.Fields[0] = value; }
        }

        /// <summary>
        /// Gets or sets the delivery-id field. 
        /// </summary>
        public uint DeliveryId
        {
            get { return this.Fields[1] == null ? uint.MinValue : (uint)this.Fields[1]; }
            set { this.Fields[1] = value; }
        }

        /// <summary>
        /// Gets or sets the delivery-tag field.
        /// </summary>
        public byte[] DeliveryTag
        {
            get { return (byte[])this.Fields[2]; }
            set { this.Fields[2] = value; }
        }

        /// <summary>
        /// Gets or sets the message-format field.
        /// </summary>
        public uint MessageFormat
        {
            get { return this.Fields[3] == null ? uint.MinValue : (uint)this.Fields[3]; }
            set { this.Fields[3] = value; }
        }

        /// <summary>
        /// Gets or sets the settled field.
        /// </summary>
        public bool Settled
        {
            get { return this.Fields[4] == null ? false : (bool)this.Fields[4]; }
            set { this.Fields[4] = value; }
        }

        /// <summary>
        /// Gets or sets the more field.
        /// </summary>
        public bool More
        {
            get { return this.Fields[5] == null ? false : (bool)this.Fields[5]; }
            set { this.Fields[5] = value; }
        }

        /// <summary>
        /// Gets or sets the rcv-settle-mode field.
        /// </summary>
        public ReceiverSettleMode RcvSettleMode
        {
            get { return this.Fields[6] == null ? ReceiverSettleMode.First : (ReceiverSettleMode)this.Fields[6]; }
            set { this.Fields[6] = (byte)value; }
        }

        /// <summary>
        /// Gets or sets the state field.
        /// </summary>
        public DeliveryState State
        {
            get { return (DeliveryState)this.Fields[7]; }
            set { this.Fields[7] = value; }
        }

        /// <summary>
        /// Gets or sets the resume field.
        /// </summary>
        public bool Resume
        {
            get { return this.Fields[8] == null ? false : (bool)this.Fields[8]; }
            set { this.Fields[8] = value; }
        }

        /// <summary>
        /// Gets or sets the aborted field.
        /// </summary>
        public bool Aborted
        {
            get { return this.Fields[9] == null ? false : (bool)this.Fields[9]; }
            set { this.Fields[9] = value; }
        }

        /// <summary>
        /// Gets or sets the batchable field.
        /// </summary>
        public bool Batchable
        {
            get { return this.Fields[10] == null ? false : (bool)this.Fields[10]; }
            set { this.Fields[10] = value; }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
#if TRACE
            return this.GetDebugString(
                "transfer",
                new object[] { "handle", "delivery-id", "delivery-tag", "message-format", "settled", "more", "rcv-settle-mode", "state", "resume", "aborted", "batchable" },
                this.Fields);
#else
            return base.ToString();
#endif
        }
    }
}