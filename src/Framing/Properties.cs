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
    using System;
    using Amqp.Types;

    public sealed class Properties : DescribedList
    {
        public Properties()
            : base(Codec.Properties, 13)
        {
        }

        public string MessageId
        {
            get { return (string)this.Fields[0]; }
            set { this.Fields[0] = value; }
        }

        public byte[] UserId
        {
            get { return (byte[])this.Fields[1]; }
            set { this.Fields[1] = value; }
        }

        public string To
        {
            get { return (string)this.Fields[2]; }
            set { this.Fields[2] = value; }
        }

        public string Subject
        {
            get { return (string)this.Fields[3]; }
            set { this.Fields[3] = value; }
        }

        public string ReplyTo
        {
            get { return (string)this.Fields[4]; }
            set { this.Fields[4] = value; }
        }

        public string CorrelationId
        {
            get { return (string)this.Fields[5]; }
            set { this.Fields[5] = value; }
        }

        public Symbol ContentType
        {
            get { return (Symbol)this.Fields[6]; }
            set { this.Fields[6] = value; }
        }

        public Symbol ContentEncoding
        {
            get { return (Symbol)this.Fields[7]; }
            set { this.Fields[7] = value; }
        }

        public DateTime AbsoluteExpiryTime
        {
            get { return this.Fields[8] == null ? DateTime.MinValue : (DateTime)this.Fields[8]; }
            set { this.Fields[8] = value; }
        }

        public DateTime CreationTime
        {
            get { return this.Fields[9] == null ? DateTime.MinValue : (DateTime)this.Fields[9]; }
            set { this.Fields[9] = value; }
        }

        public string GroupId
        {
            get { return (string)this.Fields[10]; }
            set { this.Fields[10] = value; }
        }

        public uint GroupSequence
        {
            get { return this.Fields[11] == null ? uint.MinValue : (uint)this.Fields[11]; }
            set { this.Fields[11] = value; }
        }

        public string ReplyToGroupId
        {
            get { return (string)this.Fields[12]; }
            set { this.Fields[12] = value; }
        }

        public override string ToString()
        {
#if TRACE
            return this.GetDebugString(
                "properties",
                new object[] { "message-id", "user-id", "to", "subject", "reply-to", "correlation-id", "content-type", "content-encoding", "absolute-expiry-time", "creation-time", "group-id", "group-sequence", "reply-to-group-id" },
                this.Fields);
#else
            return base.ToString();
#endif
        }
    }
}