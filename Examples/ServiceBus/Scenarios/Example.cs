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

namespace ServiceBus.Scenarios
{
    using Amqp;

    abstract class Example
    {
        // Update the following with valid Service Bus namespace and SAS key info.
        // Entity should exist already under the specified Service Bus namespace.
        // The SAS policy should allow Send or Listen action for the given entity
        // or namespace as required by the sample.
        public string Namespace = "contoso.servicebus.windows.net";
        public string KeyName = "key1";
        public string KeyValue = "5znwNTZDYC39dqhFOTDtnaikd1hiuRa4XaAj3Y9kJhQ=";
        public string Entity = "myEntity";

        public Address GetAddress()
        {
            return new Address(this.Namespace, 5671, this.KeyName, this.KeyValue);
        }

        public abstract void Run();
    }
}
