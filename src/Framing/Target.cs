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
    /// The target is comprised of an address coupled with properties that determine
    /// message transfer behavior.
    /// </summary>
    public sealed class Target : DescribedList
    {
        string address;
        uint durable;
        Symbol expiryPolicy;
        uint timeout;
        bool dynamic;
        Fields dynamicNodeProperties;
        object capabilities;

        /// <summary>
        /// Initializes a target object.
        /// </summary>
        public Target()
            : base(Codec.Target, 7)
        {
        }

        /// <summary>
        /// Gets or sets the address field (index=0).
        /// </summary>
        public string Address
        {
            get { return this.GetField(0, this.address); }
            set { this.SetField(0, ref this.address, value); }
        }

        /// <summary>
        /// Gets or sets the durable field (index=1).
        /// </summary>
        public uint Durable
        {
            get { return this.GetField(1, this.durable, 0u); }
            set { this.SetField(1, ref this.durable, value); }
        }

        /// <summary>
        /// Gets or sets the expiry-policy field (index=2).
        /// </summary>
        public Symbol ExpiryPolicy
        {
            get { return this.GetField(2, this.expiryPolicy); }
            set { this.SetField(2, ref this.expiryPolicy, value); }
        }

        /// <summary>
        /// Gets or sets the timeout field (index=3).
        /// </summary>
        public uint Timeout
        {
            get { return this.GetField(3, this.timeout, 0u); }
            set { this.SetField(3, ref this.timeout, value); }
        }

        /// <summary>
        /// Gets or sets the dynamic field (index=4).
        /// </summary>
        public bool Dynamic
        {
            get { return this.GetField(4, this.dynamic, false); }
            set { this.SetField(4, ref this.dynamic, value); }
        }

        /// <summary>
        /// Gets or sets the dynamic-node-properties field (index=5).
        /// </summary>
        public Fields DynamicNodeProperties
        {
            get { return this.GetField(5, this.dynamicNodeProperties); }
            set { this.SetField(5, ref this.dynamicNodeProperties, value); }
        }

        /// <summary>
        /// Gets or sets the capabilities field (index=6).
        /// </summary>
        public Symbol[] Capabilities
        {
            get { return HasField(6) ? Codec.GetSymbolMultiple(ref this.capabilities) : null; }
            set { this.SetField(6, ref this.capabilities, value); }
        }

        internal override void WriteField(ByteBuffer buffer, int index)
        {
            switch (index)
            {
                case 0:
                    Encoder.WriteString(buffer, this.address, true);
                    break;
                case 1:
                    Encoder.WriteUInt(buffer, this.durable, true);
                    break;
                case 2:
                    Encoder.WriteSymbol(buffer, this.expiryPolicy, true);
                    break;
                case 3:
                    Encoder.WriteUInt(buffer, this.timeout, true);
                    break;
                case 4:
                    Encoder.WriteBoolean(buffer, this.dynamic, true);
                    break;
                case 5:
                    Encoder.WriteMap(buffer, this.dynamicNodeProperties, true);
                    break;
                case 6:
                    Encoder.WriteObject(buffer, this.capabilities, true);
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
                    this.address = Encoder.ReadString(buffer, formatCode);
                    break;
                case 1:
                    this.durable = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 2:
                    this.expiryPolicy = Encoder.ReadSymbol(buffer, formatCode);
                    break;
                case 3:
                    this.timeout = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 4:
                    this.dynamic = Encoder.ReadBoolean(buffer, formatCode);
                    break;
                case 5:
                    this.dynamicNodeProperties = Encoder.ReadFields(buffer, formatCode);
                    break;
                case 6:
                    this.capabilities = Encoder.ReadObject(buffer, formatCode);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
        }

#if TRACE
        /// <summary>
        /// Returns a string that represents the current target object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.GetDebugString(
                "target",
                new object[] { "address", "durable", "expiry-policy", "timeout", "dynamic", "dynamic-node-properties", "capabilities" },
                new object[] { address, durable, expiryPolicy, timeout, dynamic, dynamicNodeProperties, capabilities });
        }
#endif
    }
}