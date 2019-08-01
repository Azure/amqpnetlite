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
    /// The Begin class contains parameters to begin a session in a connection.
    /// </summary>
    public sealed class Begin : DescribedList
    {
        /// <summary>
        /// Initializes a Begin object.
        /// </summary>
        public Begin()
            : base(Codec.Begin, 8)
        {
        }

        private ushort? remoteChannel;
        /// <summary>
        /// Gets or sets the remote-channel field.
        /// </summary>
        public ushort RemoteChannel
        {
            get { return this.remoteChannel == null ? ushort.MaxValue : this.remoteChannel.Value; }
            set { this.remoteChannel = value; }
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

        private uint? incomingWindow;
        /// <summary>
        /// Gets or sets the incoming-window field.
        /// </summary>
        public uint IncomingWindow
        {
            get { return this.incomingWindow == null ? uint.MaxValue : this.incomingWindow.Value; }
            set { this.incomingWindow = value; }
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

        private uint? handleMax;
        /// <summary>
        /// Gets or sets the handle-max field.
        /// </summary>
        public uint HandleMax
        {
            get { return this.handleMax == null ? uint.MaxValue : this.handleMax.Value; }
            set { this.handleMax = value; }
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
            get { return this.properties; }
            set { this.properties = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.remoteChannel = Encoder.ReadUShort(buffer);
            }

            if (count-- > 0)
            {
                this.nextOutgoingId = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.incomingWindow = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.outgoingWindow = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.handleMax = Encoder.ReadUInt(buffer);
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
            Encoder.WriteUShort(buffer, remoteChannel);
            Encoder.WriteUInt(buffer, nextOutgoingId, true);
            Encoder.WriteUInt(buffer, incomingWindow, true);
            Encoder.WriteUInt(buffer, outgoingWindow, true);
            Encoder.WriteUInt(buffer, handleMax, true);
            Encoder.WriteObject(buffer, offeredCapabilities, true);
            Encoder.WriteObject(buffer, desiredCapabilities, true);
            Encoder.WriteMap(buffer, properties, true);
        }

        /// <summary>
        /// Returns a string that represents the current begin object.
        /// </summary>
        public override string ToString()
        {
#if TRACE
            return this.GetDebugString(
                "begin",
                new object[] { "remote-channel", "next-outgoing-id", "incoming-window", "outgoing-window", "handle-max", "offered-capabilities", "desired-capabilities", "properties" },
                new object[] {remoteChannel, nextOutgoingId, incomingWindow, outgoingWindow, handleMax, offeredCapabilities, desiredCapabilities, properties});
#else
            return base.ToString();
#endif
        }
    }
}