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
    /// The source is comprised of an address coupled with properties that determine
    /// message transfer behavior.
    /// </summary>
    public sealed class Source : DescribedList
    {
        /// <summary>
        /// Initializes a source object.
        /// </summary>
        public Source()
            : base(Codec.Source, 11)
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

        private Symbol distributionMode;
        /// <summary>
        /// Gets or sets the distribution-mode field.
        /// </summary>
        public Symbol DistributionMode
        {
            get { return this.distributionMode; }
            set { this.distributionMode = value; }
        }

        private Map filterSet;
        /// <summary>
        /// Gets or sets the filter field.
        /// </summary>
        public Map FilterSet
        {
            get { return this.filterSet; }
            set { this.filterSet = value; }
        }

        private Outcome defaultOutcome;
        /// <summary>
        /// Gets or sets the default-outcome field.
        /// </summary>
        public Outcome DefaultOutcome
        {
            get { return this.defaultOutcome; }
            set { this.defaultOutcome = value; }
        }

        private object outcomes;
        /// <summary>
        /// Gets or sets the outcomes field.
        /// </summary>
        public Symbol[] Outcomes
        {
            get { return Codec.GetSymbolMultiple(ref this.outcomes); }
            set { this.outcomes = value; }
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
                this.distributionMode = Encoder.ReadSymbol(buffer);
            }

            if (count-- > 0)
            {
                this.filterSet = Encoder.ReadMap(buffer);
            }

            if (count-- > 0)
            {
                this.defaultOutcome = (Outcome)Encoder.ReadObject(buffer);
            }

            if (count-- > 0)
            {
                this.outcomes = Encoder.ReadObject(buffer);
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
            Encoder.WriteSymbol(buffer, distributionMode, true);
            Encoder.WriteMap(buffer, filterSet, true);
            Encoder.WriteObject(buffer, defaultOutcome, true);
            Encoder.WriteObject(buffer, outcomes, true);
            Encoder.WriteObject(buffer, capabilities, true);
        }

#if TRACE
        /// <summary>
        /// Returns a string that represents the current source object.
        /// </summary>
        public override string ToString()
        {
            return this.GetDebugString(
                "source",
                new object[] { "address", "durable", "expiry-policy", "timeout", "dynamic", "dynamic-node-properties", "distribution-mode", "filter", "default-outcome", "outcomes", "capabilities" },
                new object[] { address, durable, expiryPolicy, timeout, dynamic, dynamicNodeProperties, distributionMode, filterSet, defaultOutcome, outcomes, capabilities });
        }
#endif
    }
}