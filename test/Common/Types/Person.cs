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
    using System.Collections.Generic;
    using global::Amqp.Serialization;

    [AmqpContract(Name = "test.amqp:person", Code = 0x0000123400000000)]
    [AmqpProvides(typeof(Student))]
    [AmqpProvides(typeof(Teacher))]
    class Person
    {
        public Person()
        {
        }

        public Person(string name)
        {
            this.Name = name;
        }

        public int Version
        {
            get;
            protected set;
        }

        [AmqpMember(Order = 1)]
        public string Name
        {
            get;
            private set;
        }

        [AmqpMember(Order = 2)]
        public int Age
        {
            get;
            set;
        }

        [AmqpMember(Order = 3)]
        public DateTime? DateOfBirth;

        public IDictionary<string, object> Properties
        {
            get
            {
                if (this.properties == null)
                {
                    this.properties = new Dictionary<string, object>();
                }

                return this.properties;
            }
        }

        [AmqpMember(Order = 8)]
        Dictionary<string, object> properties;

        [OnDeserialized]
        void OnDesrialized()
        {
            this.Age = this.Age + 1;
        }
    }
}
