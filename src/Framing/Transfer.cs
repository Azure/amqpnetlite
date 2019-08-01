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
            get { return this.deliveryId != null; }
        }

        private uint? handle;
        /// <summary>
        /// Gets or sets the handle field.
        /// </summary>
        public uint Handle
        {
            get { return this.handle == null ? uint.MaxValue : this.handle.Value; }
            set { this.handle = value; }
        }

        private uint? deliveryId;
        /// <summary>
        /// Gets or sets the delivery-id field. 
        /// </summary>
        public uint DeliveryId
        {
            get { return this.deliveryId == null ? uint.MinValue : this.deliveryId.Value; }
            set { this.deliveryId = value; }
        }

        private byte[] deliveryTag;
        /// <summary>
        /// Gets or sets the delivery-tag field.
        /// </summary>
        public byte[] DeliveryTag
        {
            get { return this.deliveryTag; }
            set { this.deliveryTag = value; }
        }

        private uint? messageFormat;
        /// <summary>
        /// Gets or sets the message-format field.
        /// </summary>
        public uint MessageFormat
        {
            get { return this.messageFormat == null ? uint.MinValue : this.messageFormat.Value; }
            set { this.messageFormat = value; }
        }

        private bool? settled;
        /// <summary>
        /// Gets or sets the settled field.
        /// </summary>
        public bool Settled
        {
            get { return this.settled == null ? false : this.settled.Value; }
            set { this.settled = value; }
        }

        private bool? more;
        /// <summary>
        /// Gets or sets the more field.
        /// </summary>
        public bool More
        {
            get { return this.more == null ? false : this.more.Value; }
            set { this.more = value; }
        }

        private ReceiverSettleMode? rcvSettleMode;
        /// <summary>
        /// Gets or sets the rcv-settle-mode field.
        /// </summary>
        public ReceiverSettleMode RcvSettleMode
        {
            get { return this.rcvSettleMode == null ? ReceiverSettleMode.First : this.rcvSettleMode.Value; }
            set { this.rcvSettleMode = value; }
        }

        private DeliveryState state;
        /// <summary>
        /// Gets or sets the state field.
        /// </summary>
        public DeliveryState State
        {
            get { return this.state; }
            set { this.state = value; }
        }

        private bool? resume;
        /// <summary>
        /// Gets or sets the resume field.
        /// </summary>
        public bool Resume
        {
            get { return this.resume == null ? false : this.resume.Value; }
            set { this.resume = value; }
        }

        private bool? aborted;
        /// <summary>
        /// Gets or sets the aborted field.
        /// </summary>
        public bool Aborted
        {
            get { return this.aborted == null ? false : this.aborted.Value; }
            set { this.aborted = value; }
        }

        private bool? batchable;
        /// <summary>
        /// Gets or sets the batchable field.
        /// </summary>
        public bool Batchable
        {
            get { return this.batchable == null ? false : this.batchable.Value; }
            set { this.batchable = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.handle = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.deliveryId = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.deliveryTag = Encoder.ReadBinary(buffer);
            }

            if (count-- > 0)
            {
                this.messageFormat = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.settled = Encoder.ReadBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.more = Encoder.ReadBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.rcvSettleMode = (ReceiverSettleMode)Encoder.ReadUByte(buffer);
            }

            if (count-- > 0)
            {
                this.state = (DeliveryState)Encoder.ReadObject(buffer);
            }

            if (count-- > 0)
            {
                this.resume = Encoder.ReadBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.aborted = Encoder.ReadBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.batchable = Encoder.ReadBoolean(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteUInt(buffer, handle, true);
            Encoder.WriteUInt(buffer, deliveryId, true);
            Encoder.WriteBinary(buffer, deliveryTag, true);
            Encoder.WriteUInt(buffer, messageFormat, true);
            Encoder.WriteBoolean(buffer, settled, true);
            Encoder.WriteBoolean(buffer, more, true);
            Encoder.WriteUByte(buffer, (byte?)rcvSettleMode);
            Encoder.WriteObject(buffer, state, true);
            Encoder.WriteBoolean(buffer, resume, true);
            Encoder.WriteBoolean(buffer, aborted, true);
            Encoder.WriteBoolean(buffer, batchable, true);
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
                new object[] { handle, deliveryId, deliveryTag, messageFormat, settled, more, rcvSettleMode, state, resume, aborted, batchable });
#else
            return base.ToString();
#endif
        }
    }
}