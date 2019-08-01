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

    sealed class Received : DeliveryState
    {
        public Received()
            : base(Codec.Received, 2)
        {
        }

        private uint? sectionNumber;
        public uint SectionNumber
        {
            get { return this.sectionNumber == null ? uint.MinValue : this.sectionNumber.Value; }
            set { this.sectionNumber = value; }
        }

        private ulong? sectionOffset;
        public ulong SectionOffset
        {
            get { return this.sectionOffset == null ? ulong.MinValue : this.sectionOffset.Value; }
            set { this.sectionOffset = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.sectionNumber = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.sectionOffset = Encoder.ReadULong(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteUInt(buffer, sectionNumber, true);
            Encoder.WriteULong(buffer, sectionOffset, true);
        }

        public override string ToString()
        {
#if TRACE
            return this.GetDebugString(
                "received",
                new object[] { "section-number", "section-offset" },
                new object[] { sectionNumber, sectionOffset });
#else
            return base.ToString();
#endif
        }
    }
}