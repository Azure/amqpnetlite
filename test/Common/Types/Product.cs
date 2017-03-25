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

    enum Category : sbyte
    {
        Unspecified,
        Electronic,
        Housewares,
        Sports,
        Food,
        Personal,
        Automotive
    }

    [AmqpContract(Name = "test.amqp:product", Encoding = EncodingType.Map)]
    class Product
    {
        [AmqpMember]
        public string Name;

        [AmqpMember]
        public double Price;

        [AmqpMember]
        public long Weight;

        [AmqpMember]
        public Specification Specification { get; set; }

        [AmqpMember]
        public Category Category { get; set; }

        public Dictionary<string, string> Properties;

        [OnSerializing]
        void OnSerializing()
        {
            if (this.Properties == null)
            {
                this.Properties = new Dictionary<string, string>();
            }

            this.Properties["OnSerializing"] = "true";
        }

        [OnSerialized]
        void OnSerialized()
        {
            this.Properties["OnSerialized"] = "true";
        }

        [OnDeserializing]
        void OnDeserializing()
        {
            if (this.Properties == null)
            {
                this.Properties = new Dictionary<string, string>();
            }

            this.Properties["OnDeserializing"] = "true";
        }

        [OnDeserialized]
        void OnDeserialized()
        {
            this.Properties["OnDeserialized"] = "true";
        }
    }
}
