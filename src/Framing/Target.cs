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
        /// <summary>
        /// Initializes a target object.
        /// </summary>
        public Target()
            : base(Codec.Target, 7)
        {
        }

        private string address;
        /// <summary>
        /// Gets or sets the address field.
        /// </summary>
        public string Address
        {
            get { return this.address; }
            set { this.address = value; }
        }

        private uint? durable;
        /// <summary>
        /// Gets or sets the durable field.
        /// </summary>
        public uint Durable
        {
            get { return this.durable == null ? 0u : this.durable.Value; }
            set { this.durable = value; }
        }

        private Symbol expiryPolicy;
        /// <summary>
        /// Gets or sets the expiry-policy field.
        /// </summary>
        public Symbol ExpiryPolicy
        {
            get { return this.expiryPolicy; }
            set { this.expiryPolicy = value; }
        }

        private uint? timeout;
        /// <summary>
        /// Gets or sets the timeout field.
        /// </summary>
        public uint Timeout
        {
            get { return this.timeout == null ? 0u : this.timeout.Value; }
            set { this.timeout = value; }
        }

        private bool? dynamic;
        /// <summary>
        /// Gets or sets the dynamic field.
        /// </summary>
        public bool Dynamic
        {
            get { return this.dynamic == null ? false : this.dynamic.Value; }
            set { this.dynamic = value; }
        }

        private Fields dynamicNodeProperties;
        /// <summary>
        /// Gets or sets the dynamic-node-properties field.
        /// </summary>
        public Fields DynamicNodeProperties
        {
            get { return this.dynamicNodeProperties; }
            set { this.dynamicNodeProperties = value; }
        }

        private object capabilities;
        /// <summary>
        /// Gets or sets the capabilities field.
        /// </summary>
        public Symbol[] Capabilities
        {
            get { return Codec.GetSymbolMultiple(ref this.capabilities); }
            set { this.capabilities = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.address = Encoder.ReadString(buffer);
            }

            if (count-- > 0)
            {
                this.durable = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.expiryPolicy = Encoder.ReadSymbol(buffer);
            }

            if (count-- > 0)
            {
                this.timeout = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.dynamic = Encoder.ReadBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.dynamicNodeProperties = Encoder.ReadFields(buffer);
            }
            
            if (count-- > 0)
            {
                this.capabilities = Encoder.ReadObject(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteString(buffer, address, true);
            Encoder.WriteUInt(buffer, durable, true);
            Encoder.WriteSymbol(buffer, expiryPolicy, true);
            Encoder.WriteUInt(buffer, timeout, true);
            Encoder.WriteBoolean(buffer, dynamic, true);
            Encoder.WriteMap(buffer, dynamicNodeProperties, true);
            Encoder.WriteObject(buffer, capabilities, true);
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