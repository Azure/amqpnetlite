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

    public sealed class Target : DescribedList
    {
        public Target()
            : base(Codec.Target, 7)
        {
        }

        public string Address
        {
            get { return (string)this.Fields[0]; }
            set { this.Fields[0] = value; }
        }

        public uint Durable
        {
            get { return this.Fields[1] == null ? 0u : (uint)this.Fields[1]; }
            set { this.Fields[1] = value; }
        }

        public Symbol ExpiryPolicy
        {
            get { return (Symbol)this.Fields[2]; }
            set { this.Fields[2] = value; }
        }

        public uint Timeout
        {
            get { return this.Fields[3] == null ? 0u : (uint)this.Fields[3]; }
            set { this.Fields[3] = value; }
        }

        public bool Dynamic
        {
            get { return this.Fields[4] == null ? false : (bool)this.Fields[4]; }
            set { this.Fields[4] = value; }
        }

        public Fields DynamicNodeProperties
        {
            get { return Amqp.Types.Fields.From(this.Fields, 5); }
            set { this.Fields[5] = value; }
        }

        public Symbol[] Capabilities
        {
            get { return Codec.GetSymbolMultiple(this.Fields, 6); }
            set { this.Fields[6] = value; }
        }

        public override string ToString()
        {
#if TRACE
            return this.GetDebugString(
                "target",
                new object[] { "address", "durable", "expiry-policy", "timeout", "dynamic", "dynamic-node-properties", "capabilities" },
                this.Fields);
#else
            return base.ToString();
#endif
        }
    }
}