﻿//  ------------------------------------------------------------------------------------
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

    [AmqpContract(Name = "test.amqp:teacher", Code = 0x0000123400000002)]
    class Teacher : Person
    {
        public Teacher() { }

        public Teacher(string name)
            : base(name)
        {
            this.Id = EmployeeId.New();
        }

        [AmqpMember(Name = "sallary", Order = 4)]
        public int Sallary;

        [AmqpMember(Order = 10)]
        public EmployeeId Id
        {
            get;
            private set;
        }

        [AmqpMember(Order = 11)]
        public Dictionary<int, string> Classes
        {
            get;
            set;
        }

        [System.Runtime.Serialization.OnDeserialized]
        void OnDesrialized()
        {
            this.Sallary *= 2;
        }
    }
}
