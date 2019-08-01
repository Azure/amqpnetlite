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
    /// The Open class defines the connection negotiation parameters.
    /// </summary>
    public sealed class Open : DescribedList
    {
        /// <summary>
        /// Initializes the Open object.
        /// </summary>
        public Open()
            : base(Codec.Open, 10)
        {
        }

        private string containerId;
        /// <summary>
        /// Gets or sets the container-id field.
        /// </summary>
        public string ContainerId
        {
            get { return this.containerId; }
            set { this.containerId = value; }
        }

        private string hostName;
        /// <summary>
        /// Gets or sets the hostname field.
        /// </summary>
        public string HostName
        {
            get { return this.hostName; }
            set { this.hostName = value; }
        }

        private uint? maxFrameSize;
        /// <summary>
        /// Gets or sets the max-frame-size field.
        /// </summary>
        public uint MaxFrameSize
        {
            get { return this.maxFrameSize == null ? uint.MaxValue : this.maxFrameSize.Value; }
            set { this.maxFrameSize = value; }
        }

        private ushort? channelMax;
        /// <summary>
        /// Gets or sets the channel-max field.
        /// </summary>
        public ushort ChannelMax
        {
            get { return this.channelMax == null ? ushort.MaxValue : this.channelMax.Value; }
            set { this.channelMax = value; }
        }

        private uint? idleTimeOut;
        /// <summary>
        /// Gets or sets the idle-time-out field.
        /// </summary>
        public uint IdleTimeOut
        {
            get { return this.idleTimeOut == null ? 0 : this.idleTimeOut.Value; }
            set { this.idleTimeOut = value; }
        }

        private object outgoingLocales;
        /// <summary>
        /// Gets or sets the outgoing-locales field.
        /// </summary>
        public Symbol[] OutgoingLocales
        {
            get { return Codec.GetSymbolMultiple(ref this.outgoingLocales); }
            set { this.outgoingLocales = value; }
        }

        private object incomingLocales;
        /// <summary>
        /// Gets or sets the incoming-locales field.
        /// </summary>
        public Symbol[] IncomingLocales
        {
            get { return Codec.GetSymbolMultiple(ref this.incomingLocales); }
            set { this.incomingLocales = value; }
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
                this.containerId = Encoder.ReadString(buffer);
            }

            if (count-- > 0)
            {
                this.hostName = Encoder.ReadString(buffer);
            }

            if (count-- > 0)
            {
                this.maxFrameSize = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.channelMax = Encoder.ReadUShort(buffer);
            }

            if (count-- > 0)
            {
                this.idleTimeOut = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.outgoingLocales = Encoder.ReadObject(buffer);
            }

            if (count-- > 0)
            {
                this.incomingLocales = Encoder.ReadObject(buffer);
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
            Encoder.WriteString(buffer, containerId, true);
            Encoder.WriteString(buffer, hostName, true);
            Encoder.WriteUInt(buffer, maxFrameSize, true);
            Encoder.WriteUShort(buffer, channelMax);
            Encoder.WriteUInt(buffer, idleTimeOut, true);
            Encoder.WriteObject(buffer, outgoingLocales, true);
            Encoder.WriteObject(buffer, incomingLocales, true);
            Encoder.WriteObject(buffer, offeredCapabilities, true);
            Encoder.WriteObject(buffer, desiredCapabilities, true);
            Encoder.WriteMap(buffer, properties, true);
        }

#if TRACE
        /// <summary>
        /// Returns a string that represents the current open object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.GetDebugString(
                "open",
                new object[] { "container-id", "host-name", "max-frame-size", "channel-max", "idle-time-out", "outgoing-locales", "incoming-locales", "offered-capabilities", "desired-capabilities", "properties" },
                new object[] {containerId, hostName, maxFrameSize, channelMax, idleTimeOut, outgoingLocales, incomingLocales, offeredCapabilities, desiredCapabilities, properties});
        }
#endif
    }
}