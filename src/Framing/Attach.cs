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
    /// The Attach class defines an attach frame to attach a Link Endpoint to the Session.
    /// </summary>
    public sealed class Attach : DescribedList
    {
        /// <summary>
        /// Initializes an attach object.
        /// </summary>
        public Attach()
            : base(Codec.Attach, 14)
        {
        }

        private string linkName;
        /// <summary>
        /// Gets or sets the name field.
        /// </summary>
        public string LinkName
        {
            get { return this.linkName; }
            set { this.linkName = value; }
        }

        private uint? handle;
        /// <summary>
        /// Gets or sets the handle field.
        /// </summary>
        public uint Handle
        {
            get { return this.handle == null ? uint.MinValue : this.handle.Value; }
            set { this.handle = value; }
        }

        private bool? role;
        /// <summary>
        /// Gets or sets the role field.
        /// </summary>
        public bool Role
        {
            get { return this.role == null ? false : this.role.Value; }
            set { this.role = value; }
        }

        private SenderSettleMode? sndSettleMode;
        /// <summary>
        /// Gets or sets the snd-settle-mode field.
        /// </summary>
        public SenderSettleMode SndSettleMode
        {
            get { return this.sndSettleMode == null ? SenderSettleMode.Unsettled : this.sndSettleMode.Value; }
            set { this.sndSettleMode = value; }
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

        private object source;
        /// <summary>
        /// Gets or sets the source field.
        /// </summary>
        public object Source
        {
            get { return this.source; }
            set { this.source = value; }
        }

        private object target;
        /// <summary>
        /// Gets or sets the target field.
        /// </summary>
        public object Target
        {
            get { return this.target; }
            set { this.target = value; }
        }

        private Map unsettled;
        /// <summary>
        /// Gets or sets the unsettled field.
        /// </summary>
        public Map Unsettled
        {
            get { return this.unsettled; }
            set { this.unsettled = value; }
        }

        private bool? incompleteUnsettled;
        /// <summary>
        /// Gets or sets the incomplete-unsettled field.
        /// </summary>
        public bool IncompleteUnsettled
        {
            get { return this.incompleteUnsettled == null ? false : this.incompleteUnsettled.Value; }
            set { this.incompleteUnsettled = value; }
        }

        private uint? initialDeliveryCount;
        /// <summary>
        /// Gets or sets the initial-delivery-count field.
        /// </summary>
        public uint InitialDeliveryCount
        {
            get { return this.initialDeliveryCount == null ? uint.MinValue : this.initialDeliveryCount.Value; }
            set { this.initialDeliveryCount = value; }
        }

        private ulong? maxMessageSize;
        /// <summary>
        /// Gets or sets the max-message-size field.
        /// </summary>
        public ulong MaxMessageSize
        {
            get { return this.maxMessageSize == null ? ulong.MaxValue : (ulong)this.maxMessageSize; }
            set { this.maxMessageSize = value; }
        }

        private object offeredCapabilities;
        /// <summary>
        /// Gets or sets the offered-capabilities field.
        /// </summary>
        public Symbol[] OfferedCapabilities
        {
            get { return Codec.GetSymbolMultiple(ref this.offeredCapabilities); }
            set { this.offeredCapabilities = value; }
        }

        private object desiredCapabilities;
        /// <summary>
        /// Gets or sets the desired-capabilities field.
        /// </summary>
        public Symbol[] DesiredCapabilities
        {
            get { return Codec.GetSymbolMultiple(ref this.desiredCapabilities); }
            set { this.desiredCapabilities = value; }
        }

        private Fields properties;
        /// <summary>
        /// Gets or sets the properties field.
        /// </summary>
        public Fields Properties
        {
            get { return properties; }
            set { this.properties = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.linkName = Encoder.ReadString(buffer);
            }

            if (count-- > 0)
            {
                this.handle = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.role = Encoder.ReadBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.sndSettleMode = (SenderSettleMode)Encoder.ReadUByte(buffer);
            }

            if (count-- > 0)
            {
                this.rcvSettleMode = (ReceiverSettleMode)Encoder.ReadUByte(buffer);
            }

            if (count-- > 0)
            {
                this.source = Encoder.ReadObject(buffer);
            }

            if (count-- > 0)
            {
                this.target = Encoder.ReadObject(buffer);
            }

            if (count-- > 0)
            {
                this.unsettled = Encoder.ReadMap(buffer);
            }

            if (count-- > 0)
            {
                this.incompleteUnsettled = Encoder.ReadBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.initialDeliveryCount = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.maxMessageSize = Encoder.ReadULong(buffer);
            }

            if (count-- > 0)
            {
                this.offeredCapabilities = Encoder.ReadObject(buffer);
            }

            if (count-- > 0)
            {
                this.desiredCapabilities = Encoder.ReadObject(buffer);
            }

            if (count-- > 0)
            {
                this.properties = Encoder.ReadFields(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteString(buffer, linkName, true);
            Encoder.WriteUInt(buffer, handle, true);
            Encoder.WriteBoolean(buffer, role, true);
            Encoder.WriteUByte(buffer, (byte?)sndSettleMode);
            Encoder.WriteUByte(buffer, (byte?)rcvSettleMode);
            Encoder.WriteObject(buffer, source, true);
            Encoder.WriteObject(buffer, target, true);
            Encoder.WriteMap(buffer, unsettled, true);
            Encoder.WriteBoolean(buffer, incompleteUnsettled, true);
            Encoder.WriteUInt(buffer, initialDeliveryCount, true);
            Encoder.WriteULong(buffer, maxMessageSize, true);
            Encoder.WriteObject(buffer, offeredCapabilities, true);
            Encoder.WriteObject(buffer, desiredCapabilities, true);
            Encoder.WriteMap(buffer, properties, true);
        }

#if TRACE
        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.GetDebugString(
                "attach",
                new object[] { "name", "handle", "role", "snd-settle-mode", "rcv-settle-mode", "source", "target", "unsettled", "incomplete-unsettled", "initial-delivery-count", "max-message-size", "offered-capabilities", "desired-capabilities", "properties" },
                new object[] { linkName, handle, role, sndSettleMode, rcvSettleMode, source, target, unsettled, incompleteUnsettled, initialDeliveryCount, maxMessageSize, offeredCapabilities, desiredCapabilities, properties });
        }
#endif
    }
}