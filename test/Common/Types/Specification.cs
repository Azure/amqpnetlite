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

    [AmqpContract(Name = "test.amqp:specification", Encoding = EncodingType.Map)]
    [AmqpProvides(typeof(ComputerSpecification))]
    [AmqpProvides(typeof(CarSpecification))]
    abstract class Specification
    {
        [AmqpMember]
        public string Description { get; set; }
    }

    [AmqpContract(Name = "test.amqp:computer-specification", Encoding = EncodingType.Map)]
    class ComputerSpecification : Specification
    {
        [AmqpMember]
        public int Cores;

        [AmqpMember]
        public int RamSize;
    }

    [AmqpContract(Name = "test.amqp:automotive-specification", Encoding = EncodingType.Map)]
    class CarSpecification : Specification
    {
        [AmqpMember]
        public string Engine;

        [AmqpMember]
        public int HorsePower;
    }
}
