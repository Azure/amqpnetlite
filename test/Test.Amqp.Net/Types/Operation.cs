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

    [AmqpContract(Encoding = EncodingType.SimpleMap)]
    abstract class Operation
    {
        [AmqpMember]
        public string Name;

        [AmqpMember]
        public int Version { get; set; }
    }

    [AmqpContract(Encoding = EncodingType.SimpleMap)]
    class AddOperation : Operation
    {
        [AmqpMember]
        public int Param1;

        [AmqpMember]
        public int Param2;
    }

    [AmqpContract(Encoding = EncodingType.SimpleMap)]
    class SquareRootOperation : Operation
    {
        [AmqpMember]
        public long Param;
    }

    [AmqpContract(Encoding = EncodingType.SimpleMap)]
    class MultiOperation : Operation
    {
        [AmqpMember]
        public string Instruction { get; set; }

        [AmqpMember]
        public AddOperation Add { get; set; }

        [AmqpMember]
        public SquareRootOperation SquareRoot { get; set; }
    }
}
