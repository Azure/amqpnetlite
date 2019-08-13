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
        uint nextIncomingId;
        uint incomingWindow;
        uint nextOutgoingId;
        uint outgoingWindow;
        uint handle;
        uint deliveryCount;
        uint linkCredit;
        uint available;
        bool drain;
        bool echo;
        Fields properties;

        /// <summary>
        /// Initializes a flow object.
        /// </summary>
        public Flow()
            : base(Codec.Flow, 11)
        {
        }

        /// <summary>
        /// Indicates if handle field was defined (index=0).
        /// </summary>
        public bool HasHandle
        {
            get { return this.HasField(4); }
        }

        /// <summary>
        /// Gets or sets the next-incoming-id field (index=1).
        /// </summary>
        public uint NextIncomingId
        {
            get { return this.GetField(0, this.nextIncomingId, uint.MinValue); }
            set { this.SetField(0, ref this.nextIncomingId, value); }
        }

        /// <summary>
        /// Gets or sets the incoming-window field (index=1).
        /// </summary>
        public uint IncomingWindow
        {
            get { return this.GetField(1, this.incomingWindow, uint.MaxValue); }
            set { this.SetField(1, ref this.incomingWindow, value); }
        }

        /// <summary>
        /// Gets or sets the next-outgoing-id field (index=2).
        /// </summary>
        public uint NextOutgoingId
        {
            get { return this.GetField(2, this.nextOutgoingId, uint.MinValue); }
            set { this.SetField(2, ref this.nextOutgoingId, value); }
        }

        /// <summary>
        /// Gets or sets the outgoing-window field (index=3).
        /// </summary>
        public uint OutgoingWindow
        {
            get { return this.GetField(3, this.outgoingWindow, uint.MaxValue); }
            set { this.SetField(3, ref this.outgoingWindow, value); }
        }

        /// <summary>
        /// Gets or sets the handle field (index=4).
        /// </summary>
        public uint Handle
        {
            get { return this.GetField(4, this.handle, uint.MaxValue); }
            set { this.SetField(4, ref this.handle, value); }
        }

        /// <summary>
        /// Gets or sets the delivery-count field (index=5).
        /// </summary>
        public uint DeliveryCount
        {
            get { return this.GetField(5, this.deliveryCount, uint.MinValue); }
            set { this.SetField(5, ref this.deliveryCount, value); }
        }

        /// <summary>
        /// Gets or sets the link-credit field (index=6).
        /// </summary>
        public uint LinkCredit
        {
            get { return this.GetField(6, this.linkCredit, uint.MinValue); }
            set { this.SetField(6, ref this.linkCredit, value); }
        }

        /// <summary>
        /// Gets or sets the available field (index=7).
        /// </summary>
        public uint Available
        {
            get { return this.GetField(7, this.available, uint.MinValue); }
            set { this.SetField(7, ref this.available, value); }
        }

        /// <summary>
        /// Gets or sets the drain field (index=8).
        /// </summary>
        public bool Drain
        {
            get { return this.GetField(8, this.drain, false); }
            set { this.SetField(8, ref this.drain, value); }
        }

        /// <summary>
        /// Gets or sets the echo field (index=9).
        /// </summary>
        public bool Echo
        {
            get { return this.GetField(9, this.echo, false); }
            set { this.SetField(9, ref this.echo, value); }
        }

        /// <summary>
        /// Gets or sets the properties field (index=10).
        /// </summary>
        public Fields Properties
        {
            get { return this.GetField(10, this.properties); }
            set { this.SetField(10, ref this.properties, value); }
        }

        internal override void WriteField(ByteBuffer buffer, int index)
        {
            switch (index)
            {
                case 0:
                    Encoder.WriteUInt(buffer, this.nextIncomingId, true);
                    break;
                case 1:
                    Encoder.WriteUInt(buffer, this.incomingWindow, true);
                    break;
                case 2:
                    Encoder.WriteUInt(buffer, this.nextOutgoingId, true);
                    break;
                case 3:
                    Encoder.WriteUInt(buffer, this.outgoingWindow, true);
                    break;
                case 4:
                    Encoder.WriteUInt(buffer, this.handle, true);
                    break;
                case 5:
                    Encoder.WriteUInt(buffer, this.deliveryCount, true);
                    break;
                case 6:
                    Encoder.WriteUInt(buffer, this.linkCredit, true);
                    break;
                case 7:
                    Encoder.WriteUInt(buffer, this.available, true);
                    break;
                case 8:
                    Encoder.WriteBoolean(buffer, this.drain, true);
                    break;
                case 9:
                    Encoder.WriteBoolean(buffer, this.echo, true);
                    break;
                case 10:
                    Encoder.WriteMap(buffer, this.properties, true);
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
                    this.nextIncomingId = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 1:
                    this.incomingWindow = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 2:
                    this.nextOutgoingId = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 3:
                    this.outgoingWindow = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 4:
                    this.handle = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 5:
                    this.deliveryCount = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 6:
                    this.linkCredit = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 7:
                    this.available = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 8:
                    this.drain = Encoder.ReadBoolean(buffer, formatCode);
                    break;
                case 9:
                    this.echo = Encoder.ReadBoolean(buffer, formatCode);
                    break;
                case 10:
                    this.properties = Encoder.ReadFields(buffer, formatCode);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
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