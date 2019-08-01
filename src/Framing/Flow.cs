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
    /// The Flow class defines a flow frame that updates the flow state for the specified link.
    /// </summary>
    public sealed class Flow : DescribedList
    {
        /// <summary>
        /// Initializes a flow object.
        /// </summary>
        public Flow()
            : base(Codec.Flow, 11)
        {
        }

        /// <summary>
        /// Indicates if handle field was defined.
        /// </summary>
        public bool HasHandle
        {
            get { return this.handle != null; }
        }

        private uint? nextIncomingId;
        /// <summary>
        /// Gets or sets the next-incoming-id field. 
        /// </summary>
        public uint NextIncomingId
        {
            get { return this.nextIncomingId == null ? uint.MinValue : this.nextIncomingId.Value; }
            set { this.nextIncomingId = value; }
        }

        private uint? incomingWindow;
        /// <summary>
        /// Gets or sets the incoming-window field. 
        /// </summary>
        public uint IncomingWindow
        {
            get { return this.incomingWindow == null ? uint.MaxValue : this.incomingWindow.Value; }
            set { this.incomingWindow = value; }
        }

        private uint? nextOutgoingId;
        /// <summary>
        /// Gets or sets the next-outgoing-id field. 
        /// </summary>
        public uint NextOutgoingId
        {
            get { return this.nextOutgoingId == null ? uint.MinValue : this.nextOutgoingId.Value; }
            set { this.nextOutgoingId = value; }
        }

        private uint? outgoingWindow;
        /// <summary>
        /// Gets or sets the outgoing-window field.
        /// </summary>
        public uint OutgoingWindow
        {
            get { return this.outgoingWindow == null ? uint.MaxValue : this.outgoingWindow.Value; }
            set { this.outgoingWindow = value; }
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

        private uint? deliveryCount;
        /// <summary>
        /// Gets or sets the delivery-count field.
        /// </summary>
        public uint DeliveryCount
        {
            get { return this.deliveryCount == null ? uint.MinValue : this.deliveryCount.Value; }
            set { this.deliveryCount = value; }
        }

        private uint? linkCredit;
        /// <summary>
        /// Gets or sets the link-credit field. 
        /// </summary>
        public uint LinkCredit
        {
            get { return this.linkCredit == null ? uint.MinValue : this.linkCredit.Value; }
            set { this.linkCredit = value; }
        }

        private uint? available;
        /// <summary>
        /// Gets or sets the available field.
        /// </summary>
        public uint Available
        {
            get { return this.available == null ? uint.MinValue : this.available.Value; }
            set { this.available = value; }
        }

        private bool? drain;
        /// <summary>
        /// Gets or sets the drain field.
        /// </summary>
        public bool Drain
        {
            get { return this.drain == null ? false : this.drain.Value; }
            set { this.drain = value; }
        }

        private bool? echo;
        /// <summary>
        /// Gets or sets the echo field.
        /// </summary>
        public bool Echo
        {
            get { return this.echo == null ? false : this.echo.Value; }
            set { this.echo = value; }
        }

        private Fields properties;
        /// <summary>
        /// Gets or sets the properties field.
        /// </summary>
        public Fields Properties
        {
            get { return this.properties; }
            set { this.properties = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.nextIncomingId = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.incomingWindow = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.nextOutgoingId = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.outgoingWindow = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.handle = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.deliveryCount = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.linkCredit = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.available = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.drain = Encoder.ReadBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.echo = Encoder.ReadBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.properties = Encoder.ReadFields(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteUInt(buffer, nextIncomingId, true);
            Encoder.WriteUInt(buffer, incomingWindow, true);
            Encoder.WriteUInt(buffer, nextOutgoingId, true);
            Encoder.WriteUInt(buffer, outgoingWindow, true);
            Encoder.WriteUInt(buffer, handle, true);
            Encoder.WriteUInt(buffer, deliveryCount, true);
            Encoder.WriteUInt(buffer, linkCredit, true);
            Encoder.WriteUInt(buffer, available, true);
            Encoder.WriteBoolean(buffer, drain, true);
            Encoder.WriteBoolean(buffer, echo, true);
            Encoder.WriteMap(buffer, properties, true);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
#if TRACE
            return this.GetDebugString(
                "flow",
                new object[] { "next-in-id", "in-window", "next-out-id", "out-window", "handle", "delivery-count", "link-credit", "available", "drain", "echo", "properties" },
                new object[] { nextIncomingId, incomingWindow, nextOutgoingId, outgoingWindow, handle, deliveryCount, linkCredit, available, drain, echo, properties});
#else
            return base.ToString();
#endif
        }
    }
}