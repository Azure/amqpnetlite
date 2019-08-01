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

        private bool? role;
        /// <summary>
        /// Gets or sets the role field.
        /// </summary>
        public bool Role
        {
            get { return this.role == null ? false : this.role.Value; }
            set { this.role = value; }
        }

        private uint? first;
        /// <summary>
        /// Gets or sets the first field.
        /// </summary>
        public uint First
        {
            get { return this.first == null ? uint.MinValue : this.first.Value; }
            set { this.first = value; }
        }

        private uint? last;
        /// <summary>
        /// Gets or sets the last field.
        /// </summary>
        public uint Last
        {
            get { return this.last == null ? this.First : this.last.Value; }
            set { this.last = value; }
        }

        private bool? settled;
        /// <summary>
        /// Gets or sets the settled field. 
        /// </summary>
        public bool Settled
        {
            get { return this.settled == null ? false : this.settled.Value; }
            set { this.settled = value; }
        }

        private DeliveryState state;
        /// <summary>
        /// Gets or sets the state field.
        /// </summary>
        public DeliveryState State
        {
            get { return this.state; }
            set { this.state = value; }
        }

        private bool? batchable;
        /// <summary>
        /// Gets or sets the batchable field 
        /// </summary>
        public bool Batchable
        {
            get { return this.batchable == null ? false : this.batchable.Value; }
            set { this.batchable = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.role = Encoder.ReadBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.first = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.last = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.settled = Encoder.ReadBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.state = (DeliveryState)Encoder.ReadObject(buffer);
            }

            if (count-- > 0)
            {
                this.batchable = Encoder.ReadBoolean(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteBoolean(buffer, role, true);
            Encoder.WriteUInt(buffer, first, true);
            Encoder.WriteUInt(buffer, last, true);
            Encoder.WriteBoolean(buffer, settled, true);
            Encoder.WriteObject(buffer, state, true);
            Encoder.WriteBoolean(buffer, batchable, true);
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
                new object[] { role, first, last, settled, state, batchable });
#else
            return base.ToString();
#endif
        }
    }
}