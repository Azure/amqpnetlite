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
    using global::Amqp.Serialization;

    // Duplicate Order values
    [AmqpContract(Name = "test.amqp:duplicate-order", Encoding = EncodingType.List)]
    class NegativeDuplicateOrder
    {
        [AmqpMember(Order = 1)]
        public int Field1;

        [AmqpMember(Order = 2)]
        public int Field2;

        [AmqpMember(Order = 1)]
        public int Field3;
    }

    // Inherited class has different EncodingType than the base class
    [AmqpContract(Encoding = EncodingType.Map)]
    [AmqpProvides(typeof(NegativeSimpleMapNoProvides))]
    abstract class NegativeWrongBaseEncodingBase
    {
        [AmqpMember]
        public string Name { get; set; }
    }

    [AmqpContract(Encoding = EncodingType.List)]
    class NegativeWrongBaseEncoding : NegativeWrongBaseEncodingBase
    {
        [AmqpMember]
        public int Field1;
    }

    // SimpleMap encoding has AmqpProvides attribute
    [AmqpContract(Encoding = EncodingType.SimpleMap)]
    [AmqpProvides(typeof(NegativeSimpleMapNoProvides))]
    abstract class NegativeSimpleMapNoProvidesBase
    {
        [AmqpMember]
        public string Name { get; set; }
    }

    [AmqpContract(Encoding = EncodingType.SimpleMap)]
    class NegativeSimpleMapNoProvides : NegativeSimpleMapNoProvidesBase
    {
        [AmqpMember]
        public int Field1;
    }

    // SimpleList encoding has AmqpProvides attribute
    [AmqpContract(Encoding = EncodingType.SimpleList)]
    [AmqpProvides(typeof(NegativeSimpleListNoProvides))]
    abstract class NegativeSimpleListNoProvidesBase
    {
        [AmqpMember]
        public string Name { get; set; }
    }

    [AmqpContract(Encoding = EncodingType.SimpleList)]
    class NegativeSimpleListNoProvides : NegativeSimpleListNoProvidesBase
    {
        [AmqpMember]
        public int Field1;
    }

    // AmqpProvides is not a contract
    [AmqpContract(Encoding = EncodingType.List)]
    [AmqpProvides(typeof(NegativeProvidesNotContract))]
    abstract class NegativeProvidesNotContractBase
    {
        [AmqpMember]
        public string Name { get; set; }
    }

    class NegativeProvidesNotContract : NegativeProvidesNotContractBase
    {
        public int Field1;
    }
}

