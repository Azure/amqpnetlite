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
        ushort remoteChannel;
        uint nextOutgoingId;
        uint incomingWindow;
        uint outgoingWindow;
        uint handleMax;
        object offeredCapabilities;
        object desiredCapabilities;
        Fields properties;

        /// <summary>
        /// Initializes a Begin object.
        /// </summary>
        public Begin()
            : base(Codec.Begin, 8)
        {
        }

        /// <summary>
        /// Gets or sets the remote-channel field (index=0).
        /// </summary>
        public ushort RemoteChannel
        {
            get { return this.GetField(0, this.remoteChannel, ushort.MaxValue); }
            set { this.SetField(0, ref this.remoteChannel, value); }
        }

        /// <summary>
        /// Gets or sets the next-outgoing-id field (index=1).
        /// </summary>
        public uint NextOutgoingId
        {
            get { return this.GetField(1, this.nextOutgoingId, uint.MinValue); }
            set { this.SetField(1, ref this.nextOutgoingId, value); }
        }

        /// <summary>
        /// Gets or sets the incoming-window field (index=2).
        /// </summary>
        public uint IncomingWindow
        {
            get { return this.GetField(2, this.incomingWindow, uint.MaxValue); }
            set { this.SetField(2, ref this.incomingWindow, value); }
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
        /// Gets or sets the handle-max field (index=4).
        /// </summary>
        public uint HandleMax
        {
            get { return this.GetField(4, this.handleMax, uint.MaxValue); }
            set { this.SetField(4, ref this.handleMax, value); }
        }

        /// <summary>
        /// Gets or sets the offered-capabilities field (index=5).
        /// </summary>
        public Symbol[] OfferedCapabilities
        {
            get { return HasField(5) ? Codec.GetSymbolMultiple(ref this.offeredCapabilities) : null; }
            set { this.SetField(5, ref this.offeredCapabilities, value); }
        }

        /// <summary>
        /// Gets or sets the desired-capabilities field (index=6).
        /// </summary>
        public Symbol[] DesiredCapabilities
        {
            get { return HasField(6) ? Codec.GetSymbolMultiple(ref this.desiredCapabilities) : null; }
            set { this.SetField(6, ref this.desiredCapabilities, value); }
        }

        /// <summary>
        /// Gets or sets the properties field (index=7).
        /// </summary>
        public Fields Properties
        {
            get { return this.GetField(7, this.properties); }
            set { this.SetField(7, ref this.properties, value); }
        }

        internal override void WriteField(ByteBuffer buffer, int index)
        {
            switch (index)
            {
                case 0:
                    Encoder.WriteUShort(buffer, this.remoteChannel);
                    break;
                case 1:
                    Encoder.WriteUInt(buffer, this.nextOutgoingId, true);
                    break;
                case 2:
                    Encoder.WriteUInt(buffer, this.incomingWindow, true);
                    break;
                case 3:
                    Encoder.WriteUInt(buffer, this.outgoingWindow, true);
                    break;
                case 4:
                    Encoder.WriteUInt(buffer, this.handleMax, true);
                    break;
                case 5:
                    Encoder.WriteObject(buffer, this.offeredCapabilities);
                    break;
                case 6:
                    Encoder.WriteObject(buffer, this.desiredCapabilities);
                    break;
                case 7:
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
                    this.remoteChannel = Encoder.ReadUShort(buffer, formatCode);
                    break;
                case 1:
                    this.nextOutgoingId = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 2:
                    this.incomingWindow = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 3:
                    this.outgoingWindow = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 4:
                    this.handleMax = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 5:
                    this.offeredCapabilities = Encoder.ReadObject(buffer, formatCode);
                    break;
                case 6:
                    this.desiredCapabilities = Encoder.ReadObject(buffer, formatCode);
                    break;
                case 7:
                    this.properties = Encoder.ReadFields(buffer, formatCode);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
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