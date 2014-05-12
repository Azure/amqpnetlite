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
using Amqp;

namespace Test.Amqp
{
    public class UtilityTests
    {
        public void TestMethod_Address()
        {
            Address address = new Address("amqp://me:secret@my.contoso.com:1234/foo/bar");
            Assert.AreEqual("amqp", address.Scheme);
            Assert.AreEqual(false, address.UseSsl);
            Assert.AreEqual("me", address.User);
            Assert.AreEqual("secret", address.Password);
            Assert.AreEqual("my.contoso.com", address.Host);
            Assert.AreEqual(1234, address.Port);
            Assert.AreEqual("/foo/bar", address.Path);

            address = new Address("amqp://broker1/foo");
            Assert.AreEqual("amqp", address.Scheme);
            Assert.AreEqual(false, address.UseSsl);
            Assert.AreEqual(null, address.User);
            Assert.AreEqual(null, address.Password);
            Assert.AreEqual("broker1", address.Host);
            Assert.AreEqual(5672, address.Port);
            Assert.AreEqual("/foo", address.Path);

            address = new Address("amqps://broker1:5555/foo");
            Assert.AreEqual("amqps", address.Scheme);
            Assert.AreEqual(true, address.UseSsl);
            Assert.AreEqual(null, address.User);
            Assert.AreEqual(null, address.Password);
            Assert.AreEqual("broker1", address.Host);
            Assert.AreEqual(5555, address.Port);
            Assert.AreEqual("/foo", address.Path);

            address = new Address("amqps://me:@broker1/foo");
            Assert.AreEqual("amqps", address.Scheme);
            Assert.AreEqual(true, address.UseSsl);
            Assert.AreEqual("me", address.User);
            Assert.AreEqual(string.Empty, address.Password);
            Assert.AreEqual("broker1", address.Host);
            Assert.AreEqual(5671, address.Port);
            Assert.AreEqual("/foo", address.Path);
        }
    }
}
