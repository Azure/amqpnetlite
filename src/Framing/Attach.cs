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

    public sealed class Attach : DescribedList
    {
        public Attach()
            : base(Codec.Attach, 14)
        {
        }

        public string LinkName
        {
            get { return (string)this.Fields[0]; }
            set { this.Fields[0] = value; }
        }

        public uint Handle
        {
            get { return this.Fields[1] == null ? uint.MinValue : (uint)this.Fields[1]; }
            set { this.Fields[1] = value; }
        }

        public bool Role
        {
            get { return this.Fields[2] == null ? false : (bool)this.Fields[2]; }
            set { this.Fields[2] = value; }
        }

        public SenderSettleMode SndSettleMode
        {
            get { return this.Fields[3] == null ? SenderSettleMode.Unsettled : (SenderSettleMode)this.Fields[3]; }
            set { this.Fields[3] = (byte)value; }
        }

        public ReceiverSettleMode RcvSettleMode
        {
            get { return this.Fields[4] == null ? ReceiverSettleMode.First : (ReceiverSettleMode)this.Fields[4]; }
            set { this.Fields[4] = (byte)value; }
        }

        public object Source
        {
            get { return this.Fields[5]; }
            set { this.Fields[5] = value; }
        }

        public object Target
        {
            get { return this.Fields[6]; }
            set { this.Fields[6] = value; }
        }

        public Map Unsettled
        {
            get { return (Map)this.Fields[7]; }
            set { this.Fields[7] = value; }
        }

        public bool IncompleteUnsettled
        {
            get { return this.Fields[8] == null ? false : (bool)this.Fields[8]; }
            set { this.Fields[8] = value; }
        }

        public uint InitialDeliveryCount
        {
            get { return this.Fields[9] == null ? uint.MinValue : (uint)this.Fields[9]; }
            set { this.Fields[9] = value; }
        }

        public ulong MaxMessageSize
        {
            get { return this.Fields[10] == null ? ulong.MaxValue : (ulong)this.Fields[10]; }
            set { this.Fields[10] = value; }
        }

        public Symbol[] OfferedCapabilities
        {
            get { return Codec.GetSymbolMultiple(this.Fields, 11); }
            set { this.Fields[11] = value; }
        }

        public Symbol[] DesiredCapabilities
        {
            get { return Codec.GetSymbolMultiple(this.Fields, 12); }
            set { this.Fields[12] = value; }
        }

        public Fields Properties
        {
            get { return Amqp.Types.Fields.From(this.Fields, 13); }
            set { this.Fields[13] = value; }
        }
        
        public override string ToString()
        {
#if TRACE
            return this.GetDebugString(
                "attach",
                new object[] { "name", "handle", "role", "snd-settle-mode", "rcv-settle-mode", "source", "target", "unsettled", "incomplete-unsettled", "initial-delivery-count", "max-message-size", "offered-capabilities", "desired-capabilities", "properties" },
                this.Fields);
#else
            return base.ToString();
#endif
        }
    }
}