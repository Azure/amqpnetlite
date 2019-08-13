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
        uint handle;
        uint deliveryId;
        byte[] deliveryTag;
        uint messageFormat;
        bool settled;
        bool more;
        ReceiverSettleMode rcvSettleMode;
        DeliveryState state;
        bool resume;
        bool aborted;
        bool batchable;

        /// <summary>
        /// Initializes an transfer object.
        /// </summary>
        public Transfer()
            : base(Codec.Transfer, 11)
        {
        }

        /// <summary>
        /// Gets if the delivery-id field (index=1) is set.
        /// </summary>
        public bool HasDeliveryId
        {
            get { return this.HasField(1); }
        }

        /// <summary>
        /// Gets or sets the handle field (index=0).
        /// </summary>
        public uint Handle
        {
            get { return this.GetField(0, this.handle, uint.MaxValue); }
            set { this.SetField(0, ref this.handle, value); }
        }

        /// <summary>
        /// Gets or sets the delivery-id field (index=1).
        /// </summary>
        public uint DeliveryId
        {
            get { return this.GetField(1, this.deliveryId, uint.MinValue); }
            set { this.SetField(1, ref this.deliveryId, value); }
        }

        /// <summary>
        /// Gets or sets the delivery-tag field (index=2).
        /// </summary>
        public byte[] DeliveryTag
        {
            get { return this.GetField(2, this.deliveryTag); }
            set { this.SetField(2, ref this.deliveryTag, value); }
        }

        /// <summary>
        /// Gets or sets the message-format field (index=3).
        /// </summary>
        public uint MessageFormat
        {
            get { return this.GetField(3, this.messageFormat, uint.MinValue); }
            set { this.SetField(3, ref this.messageFormat, value); }
        }

        /// <summary>
        /// Gets or sets the settled field (index=4).
        /// </summary>
        public bool Settled
        {
            get { return this.GetField(4, this.settled, false); }
            set { this.SetField(4, ref this.settled, value); }
        }

        /// <summary>
        /// Gets or sets the more field (index=5).
        /// </summary>
        public bool More
        {
            get { return this.GetField(5, this.more, false); }
            set { this.SetField(5, ref this.more, value); }
        }

        /// <summary>
        /// Gets or sets the rcv-settle-mode field (index=6).
        /// </summary>
        public ReceiverSettleMode RcvSettleMode
        {
            get { return this.GetField(6, this.rcvSettleMode, ReceiverSettleMode.First); }
            set { this.SetField(6, ref this.rcvSettleMode, value); }
        }

        /// <summary>
        /// Gets or sets the state field (index=7).
        /// </summary>
        public DeliveryState State
        {
            get { return this.GetField(7, this.state); }
            set { this.SetField(7, ref this.state, value); }
        }

        /// <summary>
        /// Gets or sets the resume field (index=8).
        /// </summary>
        public bool Resume
        {
            get { return this.GetField(8, this.resume, false); }
            set { this.SetField(8, ref this.resume, value); }
        }

        /// <summary>
        /// Gets or sets the aborted field (index=9).
        /// </summary>
        public bool Aborted
        {
            get { return this.GetField(9, this.aborted, false); }
            set { this.SetField(9, ref this.aborted, value); }
        }

        /// <summary>
        /// Gets or sets the batchable field (index=10).
        /// </summary>
        public bool Batchable
        {
            get { return this.GetField(10, this.batchable, false); }
            set { this.SetField(10, ref this.batchable, value); }
        }

        internal override void WriteField(ByteBuffer buffer, int index)
        {
            switch (index)
            {
                case 0:
                    Encoder.WriteUInt(buffer, this.handle, true);
                    break;
                case 1:
                    Encoder.WriteUInt(buffer, this.deliveryId, true);
                    break;
                case 2:
                    Encoder.WriteBinary(buffer, this.deliveryTag, true);
                    break;
                case 3:
                    Encoder.WriteUInt(buffer, this.messageFormat, true);
                    break;
                case 4:
                    Encoder.WriteBoolean(buffer, this.settled, true);
                    break;
                case 5:
                    Encoder.WriteBoolean(buffer, this.more, true);
                    break;
                case 6:
                    Encoder.WriteUByte(buffer, (byte)this.rcvSettleMode);
                    break;
                case 7:
                    Encoder.WriteObject(buffer, this.state, true);
                    break;
                case 8:
                    Encoder.WriteBoolean(buffer, this.resume, true);
                    break;
                case 9:
                    Encoder.WriteBoolean(buffer, this.aborted, true);
                    break;
                case 10:
                    Encoder.WriteBoolean(buffer, this.batchable, true);
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
                    this.handle = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 1:
                    this.deliveryId = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 2:
                    this.deliveryTag = Encoder.ReadBinary(buffer, formatCode);
                    break;
                case 3:
                    this.messageFormat = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 4:
                    this.settled = Encoder.ReadBoolean(buffer, formatCode);
                    break;
                case 5:
                    this.more = Encoder.ReadBoolean(buffer, formatCode);
                    break;
                case 6:
                    this.rcvSettleMode = (ReceiverSettleMode)Encoder.ReadUByte(buffer, formatCode);
                    break;
                case 7:
                    this.state = (DeliveryState)Encoder.ReadObject(buffer, formatCode);
                    break;
                case 8:
                    this.resume = Encoder.ReadBoolean(buffer, formatCode);
                    break;
                case 9:
                    this.aborted = Encoder.ReadBoolean(buffer, formatCode);
                    break;
                case 10:
                    this.batchable = Encoder.ReadBoolean(buffer, formatCode);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
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