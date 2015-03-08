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
using Amqp;
#if !(NETMF || COMPACT_FRAMEWORK)
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace Test.Amqp
{
#if !(NETMF || COMPACT_FRAMEWORK)
    [TestClass]
#endif
    public class UtilityTests
    {
#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_Address()
        {
            Address address = new Address("amqps://broker1");
            Assert.AreEqual("amqps", address.Scheme);
            Assert.AreEqual(true, address.UseSsl);
            Assert.AreEqual(null, address.User);
            Assert.AreEqual(null, address.Password);
            Assert.AreEqual("broker1", address.Host);
            Assert.AreEqual(5671, address.Port);
            Assert.AreEqual("/", address.Path);

            address = new Address("amqp://broker1:12345");
            Assert.AreEqual("amqp", address.Scheme);
            Assert.AreEqual(false, address.UseSsl);
            Assert.AreEqual(null, address.User);
            Assert.AreEqual(null, address.Password);
            Assert.AreEqual("broker1", address.Host);
            Assert.AreEqual(12345, address.Port);
            Assert.AreEqual("/", address.Path);

            address = new Address("amqp://guest:@broker1");
            Assert.AreEqual("amqp", address.Scheme);
            Assert.AreEqual(false, address.UseSsl);
            Assert.AreEqual("guest", address.User);
            Assert.AreEqual(string.Empty, address.Password);
            Assert.AreEqual("broker1", address.Host);
            Assert.AreEqual(5672, address.Port);
            Assert.AreEqual("/", address.Path);

            address = new Address("amqp://:abc@broker1");
            Assert.AreEqual("amqp", address.Scheme);
            Assert.AreEqual(false, address.UseSsl);
            Assert.AreEqual(string.Empty, address.User);
            Assert.AreEqual("abc", address.Password);
            Assert.AreEqual("broker1", address.Host);
            Assert.AreEqual(5672, address.Port);
            Assert.AreEqual("/", address.Path);

            address = new Address("amqps://:@broker1");
            Assert.AreEqual("amqps", address.Scheme);
            Assert.AreEqual(true, address.UseSsl);
            Assert.AreEqual(string.Empty, address.User);
            Assert.AreEqual(string.Empty, address.Password);
            Assert.AreEqual("broker1", address.Host);
            Assert.AreEqual(5671, address.Port);
            Assert.AreEqual("/", address.Path);

            address = new Address("amqps://guest:pass1@broker1");
            Assert.AreEqual("amqps", address.Scheme);
            Assert.AreEqual(true, address.UseSsl);
            Assert.AreEqual("guest", address.User);
            Assert.AreEqual("pass1", address.Password);
            Assert.AreEqual("broker1", address.Host);
            Assert.AreEqual(5671, address.Port);
            Assert.AreEqual("/", address.Path);

            address = new Address("amqp://me:secret@my.contoso.com:1234/foo/bar");
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

            address = new Address("amqps://m%2fe%2f:s%21e%25c%26r%2ae%2bt%2f@my.contoso.com:1234/foo/bar");
            Assert.AreEqual("amqps", address.Scheme);
            Assert.AreEqual(true, address.UseSsl);
            Assert.AreEqual("m/e/", address.User);
            Assert.AreEqual("s!e%c&r*e+t/", address.Password);
            Assert.AreEqual("my.contoso.com", address.Host);
            Assert.AreEqual(1234, address.Port);
            Assert.AreEqual("/foo/bar", address.Path);

            address = new Address("myhost", 1234, "myuser/", "secret/", "/foo/bar", "amqps");
            Assert.AreEqual("amqps", address.Scheme);
            Assert.AreEqual(true, address.UseSsl);
            Assert.AreEqual("myuser/", address.User);
            Assert.AreEqual("secret/", address.Password);
            Assert.AreEqual("myhost", address.Host);
            Assert.AreEqual(1234, address.Port);
            Assert.AreEqual("/foo/bar", address.Path);          
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_AmqpBitConverter()
        {
            ByteBuffer buffer = new ByteBuffer(128, true);

            AmqpBitConverter.WriteByte(buffer, 0x22);
            AmqpBitConverter.WriteByte(buffer, -0x22);

            AmqpBitConverter.WriteUByte(buffer, 0x22);
            AmqpBitConverter.WriteUByte(buffer, 0xB2);

            AmqpBitConverter.WriteShort(buffer, 0x22B7);
            AmqpBitConverter.WriteShort(buffer, -0x22B7);

            AmqpBitConverter.WriteUShort(buffer, 0x22B7);
            AmqpBitConverter.WriteUShort(buffer, 0xC2B7);

            AmqpBitConverter.WriteInt(buffer, 0x340da287);
            AmqpBitConverter.WriteInt(buffer, -0x340da287);

            AmqpBitConverter.WriteUInt(buffer, 0x340da287);
            AmqpBitConverter.WriteUInt(buffer, 0xF40da287);

            AmqpBitConverter.WriteLong(buffer, 0x5d00BB9A340da287);
            AmqpBitConverter.WriteLong(buffer, -0x5d00BB9A340da287);

            AmqpBitConverter.WriteULong(buffer, 0x5d00BB9A340da287);
            AmqpBitConverter.WriteULong(buffer, 0xad00BB9A340da287);

            AmqpBitConverter.WriteFloat(buffer, 12344.4434F);
            AmqpBitConverter.WriteFloat(buffer, -12344.4434F);

            AmqpBitConverter.WriteDouble(buffer, 39432123244.44352334);
            AmqpBitConverter.WriteDouble(buffer, -39432123244.44352334);

            Guid uuid = Guid.NewGuid();
            AmqpBitConverter.WriteUuid(buffer, uuid);

            sbyte b = AmqpBitConverter.ReadByte(buffer);
            sbyte b2 = AmqpBitConverter.ReadByte(buffer);

            byte ub = AmqpBitConverter.ReadUByte(buffer);
            byte ub2 = AmqpBitConverter.ReadUByte(buffer);

            short s = AmqpBitConverter.ReadShort(buffer);
            short s2 = AmqpBitConverter.ReadShort(buffer);

            ushort us = AmqpBitConverter.ReadUShort(buffer);
            ushort us2 = AmqpBitConverter.ReadUShort(buffer);

            int i = AmqpBitConverter.ReadInt(buffer);
            int i2 = AmqpBitConverter.ReadInt(buffer);

            uint ui = AmqpBitConverter.ReadUInt(buffer);
            uint ui2 = AmqpBitConverter.ReadUInt(buffer);

            long l = AmqpBitConverter.ReadLong(buffer);
            long l2 = AmqpBitConverter.ReadLong(buffer);

            ulong ul = AmqpBitConverter.ReadULong(buffer);
            ulong ul2 = AmqpBitConverter.ReadULong(buffer);

            float f = AmqpBitConverter.ReadFloat(buffer);
            float f2 = AmqpBitConverter.ReadFloat(buffer);

            double d = AmqpBitConverter.ReadDouble(buffer);
            double d2 = AmqpBitConverter.ReadDouble(buffer);

            Guid uuid2 = AmqpBitConverter.ReadUuid(buffer);
        }
    }
}
