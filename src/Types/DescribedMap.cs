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

namespace Amqp.Types
{
    using System;
    using System.Collections;

    public abstract class DescribedMap : RestrictedDescribed
    {
        readonly Type keyType;
        Map map;

        protected DescribedMap(Descriptor descriptor, Type keyType)
            : base(descriptor)
        {
            this.keyType = keyType;
            this.map = new Map();
        }

        public Map Map { get { return this.map; } }

        public object this[object key]
        {
            get
            {
                Map.ValidateKeyType(this.keyType, key.GetType());
                return this.map[key];
            }

            set
            {
                Map.ValidateKeyType(this.keyType, key.GetType());
                this.map[key] = value;
            }
        }

        internal override void DecodeValue(ByteBuffer buffer)
        {
            this.map = Encoder.ReadMap(buffer, Encoder.ReadFormatCode(buffer));
        }

        internal override void EncodeValue(ByteBuffer buffer)
        {
            Encoder.WriteMap(buffer, this.map, true);
        }

        public override string ToString()
        {
            return this.map.ToString();
        }
    }
}
