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

    public sealed class Header : DescribedList
    {
        public Header()
            : base(Codec.Header, 5)
        {
        }

        public bool Durable
        {
            get { return this.Fields[0] == null ? false : (bool)this.Fields[0]; }
            set { this.Fields[0] = value; }
        }

        public byte Priority
        {
            get { return this.Fields[1] == null ? byte.MinValue : (byte)this.Fields[1]; }
            set { this.Fields[1] = value; }
        }

        public uint Ttl
        {
            get { return this.Fields[2] == null ? uint.MaxValue : (uint)this.Fields[2]; }
            set { this.Fields[2] = value; }
        }

        public bool FirstAcquirer
        {
            get { return this.Fields[3] == null ? false : (bool)this.Fields[3]; }
            set { this.Fields[3] = value; }
        }

        public uint DeliveryCount
        {
            get { return this.Fields[4] == null ? uint.MinValue : (uint)this.Fields[4]; }
            set { this.Fields[4] = value; }
        }

        public override string ToString()
        {
#if TRACE
            return this.GetDebugString(
                "header",
                new object[] { "durable", "priority", "ttl", "first-acquirer", "delivery-count" },
                this.Fields);
#else
            return base.ToString();
#endif
        }
    }
}