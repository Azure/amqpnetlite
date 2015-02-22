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

    public sealed class Open : DescribedList
    {
        public Open()
            : base(Codec.Open, 10)
        {
        }

        public string ContainerId
        {
            get { return (string)this.Fields[0]; }
            set { this.Fields[0] = value; }
        }

        public string HostName
        {
            get { return (string)this.Fields[1]; }
            set { this.Fields[1] = value; }
        }

        public uint MaxFrameSize
        {
            get { return this.Fields[2] == null ? uint.MaxValue : (uint)this.Fields[2]; }
            set { this.Fields[2] = value; }
        }

        public ushort ChannelMax
        {
            get { return this.Fields[3] == null ? ushort.MaxValue : (ushort)this.Fields[3]; }
            set { this.Fields[3] = value; }
        }

        public uint IdleTimeOut
        {
            get { return this.Fields[4] == null ? uint.MaxValue : (uint)this.Fields[4]; }
            set { this.Fields[4] = value; }
        }

        public Symbol[] OutgoingLocales
        {
            get { return Codec.GetSymbolMultiple(this.Fields, 5); }
            set { this.Fields[5] = value; }
        }

        public Symbol[] IncomingLocales
        {
            get { return Codec.GetSymbolMultiple(this.Fields, 6); }
            set { this.Fields[6] = value; }
        }

        public Symbol[] OfferedCapabilities
        {
            get { return Codec.GetSymbolMultiple(this.Fields, 7); }
            set { this.Fields[7] = value; }
        }

        public Symbol[] DesiredCapabilities
        {
            get { return Codec.GetSymbolMultiple(this.Fields, 8); }
            set { this.Fields[8] = value; }
        }

        public Fields Properties
        {
            get { return Amqp.Types.Fields.From(this.Fields, 9); }
            set { this.Fields[9] = value; }
        }

        public override string ToString()
        {
#if TRACE
            return this.GetDebugString(
                "open",
                new object[] { "container-id", "host-name", "max-frame-size", "channel-max", "idle-time-out", "outgoing-locales", "incoming-locales", "offered-capabilities", "desired-capabilities", "properties" },
                this.Fields);
#else
            return base.ToString();
#endif
        }
    }
}