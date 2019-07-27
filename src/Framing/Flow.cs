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
    /// The Flow class defines a flow frame that updates the flow state for the specified link.
    /// </summary>
    public sealed class Flow : DescribedList
    {
        /// <summary>
        /// Initializes a flow object.
        /// </summary>
        public Flow()
            : base(Codec.Flow, 11)
        {
        }

        /// <summary>
        /// Indicates if handle field was defined.
        /// </summary>
        public bool HasHandle
        {
            get { return this.Fields[4] != null; }
        }

        /// <summary>
        /// Gets or sets the next-incoming-id field. 
        /// </summary>
        public uint NextIncomingId
        {
            get { return this.Fields[0] == null ? uint.MinValue : (uint)this.Fields[0]; }
            set { this.Fields[0] = value; }
        }

        /// <summary>
        /// Gets or sets the incoming-window field. 
        /// </summary>
        public uint IncomingWindow
        {
            get { return this.Fields[1] == null ? uint.MaxValue : (uint)this.Fields[1]; }
            set { this.Fields[1] = value; }
        }

        /// <summary>
        /// Gets or sets the next-outgoing-id field. 
        /// </summary>
        public uint NextOutgoingId
        {
            get { return this.Fields[2] == null ? uint.MinValue : (uint)this.Fields[2]; }
            set { this.Fields[2] = value; }
        }

        /// <summary>
        /// Gets or sets the outgoing-window field.
        /// </summary>
        public uint OutgoingWindow
        {
            get { return this.Fields[3] == null ? uint.MaxValue : (uint)this.Fields[3]; }
            set { this.Fields[3] = value; }
        }

        /// <summary>
        /// Gets or sets the handle field.
        /// </summary>
        public uint Handle
        {
            get { return this.Fields[4] == null ? uint.MaxValue : (uint)this.Fields[4]; }
            set { this.Fields[4] = value; }
        }

        /// <summary>
        /// Gets or sets the delivery-count field.
        /// </summary>
        public uint DeliveryCount
        {
            get { return this.Fields[5] == null ? uint.MinValue : (uint)this.Fields[5]; }
            set { this.Fields[5] = value; }
        }

        /// <summary>
        /// Gets or sets the link-credit field. 
        /// </summary>
        public uint LinkCredit
        {
            get { return this.Fields[6] == null ? uint.MinValue : (uint)this.Fields[6]; }
            set { this.Fields[6] = value; }
        }

        /// <summary>
        /// Gets or sets the available field.
        /// </summary>
        public uint Available
        {
            get { return this.Fields[7] == null ? uint.MinValue : (uint)this.Fields[7]; }
            set { this.Fields[7] = value; }
        }

        /// <summary>
        /// Gets or sets the drain field.
        /// </summary>
        public bool Drain
        {
            get { return this.Fields[8] == null ? false : (bool)this.Fields[8]; }
            set { this.Fields[8] = value; }
        }

        /// <summary>
        /// Gets or sets the echo field.
        /// </summary>
        public bool Echo
        {
            get { return this.Fields[9] == null ? false : (bool)this.Fields[9]; }
            set { this.Fields[9] = value; }
        }

        /// <summary>
        /// Gets or sets the properties field.
        /// </summary>
        public Fields Properties
        {
            get { return Amqp.Types.Fields.From(this.Fields, 10); }
            set { this.Fields[10] = value; }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
#if TRACE
            return this.GetDebugString(
                "flow",
                new object[] { "next-in-id", "in-window", "next-out-id", "out-window", "handle", "delivery-count", "link-credit", "available", "drain", "echo", "properties" },
                this.Fields);
#else
            return base.ToString();
#endif
        }
    }
}