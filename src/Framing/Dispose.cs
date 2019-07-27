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
    /// The Dispose class defines a disposition frame to inform remote peer of delivery state changes.
    /// </summary>
    public sealed class Dispose : DescribedList
    {
        /// <summary>
        /// Initializes a dispose object.
        /// </summary>
        public Dispose()
            : base(Codec.Dispose, 6)
        {
        }

        /// <summary>
        /// Gets or sets the role field.
        /// </summary>
        public bool Role
        {
            get { return this.Fields[0] == null ? false : (bool)this.Fields[0]; }
            set { this.Fields[0] = value; }
        }

        /// <summary>
        /// Gets or sets the first field.
        /// </summary>
        public uint First
        {
            get { return this.Fields[1] == null ? uint.MinValue : (uint)this.Fields[1]; }
            set { this.Fields[1] = value; }
        }

        /// <summary>
        /// Gets or sets the last field.
        /// </summary>
        public uint Last
        {
            get { return this.Fields[2] == null ? this.First : (uint)this.Fields[2]; }
            set { this.Fields[2] = value; }
        }

        /// <summary>
        /// Gets or sets the settled field. 
        /// </summary>
        public bool Settled
        {
            get { return this.Fields[3] == null ? false : (bool)this.Fields[3]; }
            set { this.Fields[3] = value; }
        }

        /// <summary>
        /// Gets or sets the state field.
        /// </summary>
        public DeliveryState State
        {
            get { return (DeliveryState)this.Fields[4]; }
            set { this.Fields[4] = value; }
        }

        /// <summary>
        /// Gets or sets the batchable field 
        /// </summary>
        public bool Batchable
        {
            get { return this.Fields[5] == null ? false : (bool)this.Fields[5]; }
            set { this.Fields[5] = value; }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
#if TRACE
            return this.GetDebugString(
                "disposition",
                new object[] { "role", "first", "last", "settled", "state", "batchable" },
                this.Fields);
#else
            return base.ToString();
#endif
        }
    }
}