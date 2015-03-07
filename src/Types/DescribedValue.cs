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

    public class DescribedValue : Described
    {
        object descriptor;
        object value;

        public DescribedValue(object descriptor, object value)
        {
            this.descriptor = descriptor;
            this.value = value;
        }

        public object Descriptor
        {
            get { return this.descriptor; }
        }

        public object Value
        {
            get { return this.value; }
        }

        internal override void EncodeDescriptor(ByteBuffer buffer)
        {
            Encoder.WriteObject(buffer, this.descriptor);
        }

        internal override void EncodeValue(ByteBuffer buffer)
        {
            Encoder.WriteObject(buffer, this.value);
        }

        internal override void DecodeDescriptor(ByteBuffer buffer)
        {
            this.descriptor = Encoder.ReadObject(buffer);
        }

        internal override void DecodeValue(ByteBuffer buffer)
        {
            this.value = Encoder.ReadObject(buffer);
        }

#if TRACE
        public override string ToString()
        {
            return this.value == null ? "nil" : this.value.ToString();
        }
#endif
    }
}
