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
    using System.Collections.Generic;
    using global::Amqp.Serialization;

    [AmqpContract(Name = "test.amqp:student", Code = 0x0000123400000001)]
    class Student : Person
    {
        public Student() : base(null) { }

        public Student(string name)
            : base(name)
        {
        }

        [AmqpMember(Name = "address", Order = 4)]
        public StreetAddress Address;

        [AmqpMember(Name = "grades", Order = 10)]
        public List<int> Grades { get; set; }

        [OnSerializing]
        void OnSerializing()
        {
            this.Version++;
        }

        [OnSerialized]
        void OnSerialized()
        {
            this.Version++;
        }

        [OnDeserializing]
        void OnDeserializing()
        {
            this.Version++;
        }

        [OnDeserialized]
        void OnDeserialized()
        {
            this.Version++;
        }
    }
}
