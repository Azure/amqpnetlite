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

namespace Test.Amqp
{
    using System;
    using global::Amqp;
    using global::Amqp.Serialization;

    class EmployeeId : IAmqpSerializable
    {
        Guid uuid;

        EmployeeId(Guid uuid)
        {
            this.uuid = uuid;
        }

        public EmployeeId() { }

        public int EncodeSize
        {
            get { return 16; }
        }

        public void Encode(ByteBuffer buffer)
        {
            byte[] bytes = this.uuid.ToByteArray();
            buffer.Validate(true, bytes.Length);
            Buffer.BlockCopy(bytes, 0, buffer.Buffer, buffer.WritePos, bytes.Length);
            buffer.Append(bytes.Length);
        }

        public void Decode(ByteBuffer buffer)
        {
            byte[] bytes = new byte[16];
            Buffer.BlockCopy(buffer.Buffer, buffer.Offset, bytes, 0, bytes.Length);
            this.uuid = new Guid(bytes);
            buffer.Complete(bytes.Length);
        }

        public static EmployeeId New()
        {
            return new EmployeeId(Guid.NewGuid());
        }

        public override bool Equals(object obj)
        {
            return obj is EmployeeId && ((EmployeeId)obj).uuid == this.uuid;
        }

        public override int GetHashCode()
        {
            return this.uuid.GetHashCode();
        }
    }
}
