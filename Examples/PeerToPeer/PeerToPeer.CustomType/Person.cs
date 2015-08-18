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

using System;
using Amqp.Serialization;

namespace PeerToPeer.CustomType
{
    [AmqpContract]
    public class Person
    {
        [AmqpMember]
        public int Weight { get; set; }

        [AmqpMember]
        public int Height { get; set; }

        [AmqpMember]
        public string EyeColor { get; set; }

        public override string ToString()
        {
            return string.Format(
                "Weight: {0},\nHeight: {1},\nEyeColor: {2}",
                Weight,
                Height,
                EyeColor);
        }
    }
}
