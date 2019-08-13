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
        bool role;
        uint first;
        uint last;
        bool settled;
        DeliveryState state;
        bool batchable;

        /// <summary>
        /// Initializes a dispose object.
        /// </summary>
        public Dispose()
            : base(Codec.Dispose, 6)
        {
        }

        /// <summary>
        /// Gets or sets the role field (index=0).
        /// </summary>
        public bool Role
        {
            get { return this.GetField(0, this.role, false); }
            set { this.SetField(0, ref this.role, value); }
        }

        /// <summary>
        /// Gets or sets the first field (index=1).
        /// </summary>
        public uint First
        {
            get { return this.GetField(1, this.first, uint.MinValue); }
            set { this.SetField(1, ref this.first, value); }
        }

        /// <summary>
        /// Gets or sets the last field (index=2).
        /// </summary>
        public uint Last
        {
            get { return this.GetField(2, this.last, this.First); }
            set { this.SetField(2, ref this.last, value); }
        }

        /// <summary>
        /// Gets or sets the settled field (index=3). 
        /// </summary>
        public bool Settled
        {
            get { return this.GetField(3, this.settled, false); }
            set { this.SetField(3, ref this.settled, value); }
        }

        /// <summary>
        /// Gets or sets the state field (index=4).
        /// </summary>
        public DeliveryState State
        {
            get { return this.GetField(4, this.state); }
            set { this.SetField(4, ref this.state, value); }
        }

        /// <summary>
        /// Gets or sets the batchable field (index=5).
        /// </summary>
        public bool Batchable
        {
            get { return this.GetField(5, this.batchable, false); }
            set { this.SetField(5, ref this.batchable, value); }
        }

        internal override void WriteField(ByteBuffer buffer, int index)
        {
            switch (index)
            {
                case 0:
                    Encoder.WriteBoolean(buffer, this.role, true);
                    break;
                case 1:
                    Encoder.WriteUInt(buffer, this.first, true);
                    break;
                case 2:
                    Encoder.WriteUInt(buffer, this.last, true);
                    break;
                case 3:
                    Encoder.WriteBoolean(buffer, this.settled, true);
                    break;
                case 4:
                    Encoder.WriteObject(buffer, this.state);
                    break;
                case 5:
                    Encoder.WriteBoolean(buffer, this.batchable, true);
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
                    this.role = Encoder.ReadBoolean(buffer, formatCode);
                    break;
                case 1:
                    this.first = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 2:
                    this.last = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 3:
                    this.settled = Encoder.ReadBoolean(buffer, formatCode);
                    break;
                case 4:
                    this.state = (DeliveryState)Encoder.ReadObject(buffer, formatCode);
                    break;
                case 5:
                    this.batchable = Encoder.ReadBoolean(buffer, formatCode);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
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