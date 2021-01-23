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

    [AmqpContract(Name = "test.amqp:self-ref", Code = 0x0000123400000010)]
    class TestNode
    {
        [AmqpMember]
        public int Id { get; set; }

        [AmqpMember]
        public TestNode Next { get; set; }

        [AmqpMember]
        public TestNode Previous { get; set; }
    }

    [AmqpContract(Name = "test.amqp:type1", Code = 0x0000123400000011)]
    class Type1
    {
        [AmqpMember]
        public int Id { get; set; }

        [AmqpMember]
        public Type2 Next { get; set; }
    }

    [AmqpContract(Name = "test.amqp:type2", Code = 0x0000123400000012)]
    class Type2
    {
        [AmqpMember]
        public string Name { get; set; }

        public Type3 Next { get; set; }
    }

    [AmqpContract(Name = "test.amqp:type3", Code = 0x0000123400000013)]
    class Type3
    {
        [AmqpMember]
        public string Name { get; set; }

        [AmqpMember]
        public List<Type1> List { get; set; }
    }

    [AmqpContract(Name = "test.amqp:provides-base", Code = 0x0000123400000014)]
    [AmqpProvides(typeof(Provides1))]
    class ProvidesBase
    {
        [AmqpMember]
        public int Value { get; set; }
    }

    [AmqpContract(Name = "test.amqp:provides-base", Code = 0x0000123400000015)]
    class Provides1 : ProvidesBase
    {
        [AmqpMember]
        public Guid Id { get; set; }

        [AmqpMember]
        public Provides2 Next { get; set; }

        [OnDeserialized]
        void IncrementValue()
        {
            this.Value++;
        }
    }

    [AmqpContract(Name = "test.amqp:provides-base", Code = 0x0000123400000016)]
    class Provides2 : ProvidesBase
    {
        [AmqpMember]
        public string Name { get; set; }

        [OnSerializing]
        void IncrementValue()
        {
            this.Value--;
        }
    }

    [AmqpContract(Name = "test.amqp:group-list", Code = 0x0000123400000017)]
    class GroupList
    {
        [AmqpMember]
        public string Name { get; set; }

        [AmqpMember]
        public List<GroupList> SubGroups { get; set; }
    }

    [AmqpContract(Name = "test.amqp:group-array", Code = 0x0000123400000018)]
    class GroupArray
    {
        [AmqpMember]
        public string Name { get; set; }

        [AmqpMember]
        public GroupArray[] SubGroups { get; set; }
    }

    [AmqpContract(Name = "test.amqp:group-map", Code = 0x0000123400000019)]
    class GroupMap
    {
        [AmqpMember]
        public string Name { get; set; }

        [AmqpMember]
        public Dictionary<int, GroupMap> SubGroups { get; set; }
    }
}
