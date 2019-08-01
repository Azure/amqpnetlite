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
    /// The header section carries standard delivery details about the transfer of a
    /// Message through the AMQP network.
    /// </summary>
    public sealed class Header : DescribedList
    {
        /// <summary>
        /// Initializes a header object.
        /// </summary>
        public Header()
            : base(Codec.Header, 5)
        {
        }

        private bool? durable;
        /// <summary>
        /// Gets or sets the durable field.
        /// </summary>
        public bool Durable
        {
            get { return this.durable == null ? false : this.durable.Value; }
            set { this.durable = value; }
        }

        private byte? priority;
        /// <summary>
        /// Gets or sets the priority field.
        /// </summary>
        public byte Priority
        {
            get { return this.priority == null ? (byte)4 : this.priority.Value; }
            set { this.priority = value; }
        }

        private uint? ttl;
        /// <summary>
        /// Gets or sets the ttl field.
        /// </summary>
        public uint Ttl
        {
            get { return this.ttl == null ? uint.MaxValue : this.ttl.Value; }
            set { this.ttl = value; }
        }

        private bool? firstAcquirer;
        /// <summary>
        /// Gets or sets the first-acquirer field.
        /// </summary>
        public bool FirstAcquirer
        {
            get { return this.firstAcquirer == null ? false : this.firstAcquirer.Value; }
            set { this.firstAcquirer = value; }
        }

        private uint? deliveryCount;
        /// <summary>
        /// Gets or sets the delivery-count field.
        /// </summary>
        public uint DeliveryCount
        {
            get { return this.deliveryCount == null ? uint.MinValue : this.deliveryCount.Value; }
            set { this.deliveryCount = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.durable = Encoder.ReadBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.priority = Encoder.ReadUByte(buffer);
            }

            if (count-- > 0)
            {
                this.ttl = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.firstAcquirer = Encoder.ReadBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.deliveryCount = Encoder.ReadUInt(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteBoolean(buffer, durable, true);
            Encoder.WriteUByte(buffer, priority);
            Encoder.WriteUInt(buffer, ttl, true);
            Encoder.WriteBoolean(buffer, firstAcquirer, true);
            Encoder.WriteUInt(buffer, deliveryCount, true);
        }

#if TRACE
        /// <summary>
        /// Returns a string that represents the current header object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.GetDebugString(
                "header",
                new object[] { "durable", "priority", "ttl", "first-acquirer", "delivery-count" },
                new object[] {this.durable, this.priority, this.ttl, this.firstAcquirer, this.deliveryCount});
        }
#endif
    }
}