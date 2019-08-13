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
        bool durable;
        byte priority;
        uint ttl;
        bool firstAcquirer;
        uint deliveryCount;

        /// <summary>
        /// Initializes a header object.
        /// </summary>
        public Header()
            : base(Codec.Header, 5)
        {
        }

        /// <summary>
        /// Gets or sets the durable field (index=0).
        /// </summary>
        public bool Durable
        {
            get { return this.GetField(0, this.durable, false); }
            set { this.SetField(0, ref this.durable, value); }
        }

        /// <summary>
        /// Gets or sets the priority field (index=1).
        /// </summary>
        public byte Priority
        {
            get { return this.GetField(1, this.priority, (byte)4); }
            set { this.SetField(1, ref this.priority, value); }
        }

        /// <summary>
        /// Gets or sets the ttl field (index=2).
        /// </summary>
        public uint Ttl
        {
            get { return this.GetField(2, this.ttl, uint.MaxValue); }
            set { this.SetField(2, ref this.ttl, value); }
        }

        /// <summary>
        /// Gets or sets the first-acquirer field (index=3).
        /// </summary>
        public bool FirstAcquirer
        {
            get { return this.GetField(3, this.firstAcquirer, false); }
            set { this.SetField(3, ref this.firstAcquirer, value); }
        }

        /// <summary>
        /// Gets or sets the delivery-count field (index=4).
        /// </summary>
        public uint DeliveryCount
        {
            get { return this.GetField(4, this.deliveryCount, uint.MinValue); }
            set { this.SetField(4, ref this.deliveryCount, value); }
        }

        internal override void WriteField(ByteBuffer buffer, int index)
        {
            switch (index)
            {
                case 0:
                    Encoder.WriteBoolean(buffer, this.durable, true);
                    break;
                case 1:
                    Encoder.WriteUByte(buffer, this.priority);
                    break;
                case 2:
                    Encoder.WriteUInt(buffer, this.ttl, true);
                    break;
                case 3:
                    Encoder.WriteBoolean(buffer, this.firstAcquirer, true);
                    break;
                case 4:
                    Encoder.WriteUInt(buffer, this.deliveryCount, true);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
        }

        internal override void ReadField(ByteBuffer buffer, int index, byte formatCode)
        {
            switch (index)
            {
                case 0:
                    this.durable = Encoder.ReadBoolean(buffer, formatCode);
                    break;
                case 1:
                    this.priority = Encoder.ReadUByte(buffer, formatCode);
                    break;
                case 2:
                    this.ttl = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 3:
                    this.firstAcquirer = Encoder.ReadBoolean(buffer, formatCode);
                    break;
                case 4:
                    this.deliveryCount = Encoder.ReadUInt(buffer, formatCode);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
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