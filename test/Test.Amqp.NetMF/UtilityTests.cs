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
using Amqp.Types;
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

#if SMALL_MEMORY
            AmqpBitConverter.WriteByte(ref buffer, 0x22);
            AmqpBitConverter.WriteByte(ref buffer, -0x22);

            AmqpBitConverter.WriteUByte(ref buffer, 0x22);
            AmqpBitConverter.WriteUByte(ref buffer, 0xB2);

            AmqpBitConverter.WriteShort(ref buffer, 0x22B7);
            AmqpBitConverter.WriteShort(ref buffer, -0x22B7);

            AmqpBitConverter.WriteUShort(ref buffer, 0x22B7);
            AmqpBitConverter.WriteUShort(ref buffer, 0xC2B7);

            AmqpBitConverter.WriteInt(buffer, 0x340da287);
            AmqpBitConverter.WriteInt(buffer, -0x340da287);

            AmqpBitConverter.WriteUInt(buffer, 0x340da287);
            AmqpBitConverter.WriteUInt(buffer, 0xF40da287);

            AmqpBitConverter.WriteLong(ref buffer, 0x5d00BB9A340da287);
            AmqpBitConverter.WriteLong(ref buffer, -0x5d00BB9A340da287);

            AmqpBitConverter.WriteULong(ref buffer, 0x5d00BB9A340da287);
            AmqpBitConverter.WriteULong(ref buffer, 0xad00BB9A340da287);

            AmqpBitConverter.WriteFloat(buffer, 12344.4434F);
            AmqpBitConverter.WriteFloat(buffer, -12344.4434F);

            AmqpBitConverter.WriteDouble(ref buffer, 39432123244.44352334);
            AmqpBitConverter.WriteDouble(ref buffer, -39432123244.44352334);

            Guid uuid = Guid.NewGuid();
            AmqpBitConverter.WriteUuid(ref buffer, uuid);

            sbyte b = AmqpBitConverter.ReadByte(ref buffer);
            sbyte b2 = AmqpBitConverter.ReadByte(ref buffer);

            byte ub = AmqpBitConverter.ReadUByte(ref buffer);
            byte ub2 = AmqpBitConverter.ReadUByte(ref buffer);

            short s = AmqpBitConverter.ReadShort(ref buffer);
            short s2 = AmqpBitConverter.ReadShort(ref buffer);

            ushort us = AmqpBitConverter.ReadUShort(ref buffer);
            ushort us2 = AmqpBitConverter.ReadUShort(ref buffer);

            int i = AmqpBitConverter.ReadInt(ref buffer);
            int i2 = AmqpBitConverter.ReadInt(ref buffer);

            uint ui = AmqpBitConverter.ReadUInt(buffer);
            uint ui2 = AmqpBitConverter.ReadUInt(buffer);

            long l = AmqpBitConverter.ReadLong(ref buffer);
            long l2 = AmqpBitConverter.ReadLong(ref buffer);

            ulong ul = AmqpBitConverter.ReadULong(ref buffer);
            ulong ul2 = AmqpBitConverter.ReadULong(ref buffer);

            float f = AmqpBitConverter.ReadFloat(ref buffer);
            float f2 = AmqpBitConverter.ReadFloat(ref buffer);

            double d = AmqpBitConverter.ReadDouble(ref buffer);
            double d2 = AmqpBitConverter.ReadDouble(ref buffer);

            Guid uuid2 = AmqpBitConverter.ReadUuid(ref buffer);
#else
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
#endif

        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_EncoderTest()
        {
            byte[] payload = new byte[]
            {
                0x40,
                0x56, 0x01,
                0x56, 0x00,
                0x41,
                0x42,
                0x50, 0x12,
                0x50, 0xab,
                0x60, 0x12, 0xab,
                0x60, 0xab, 0x12,
                0x70, 0x12, 0xab, 0xcd, 0x89,
                0x70, 0xab, 0x12, 0x89, 0xcd,
                0x52, 0x12,
                0x52, 0xab,
                0x43,
                0x80, 0x12, 0xab, 0xcd, 0x89, 0x12, 0xab, 0xcd, 0x89,
                0x80, 0xab, 0x12, 0x89, 0xcd, 0xab, 0x12, 0x89, 0xcd,
                0x53, 0x12,
                0x53, 0xab,
                0x44,
                0x51, 0x12,
                0x51, 0xab,
                0x61, 0x12, 0xab,
                0x61, 0xab, 0x12,
                0x71, 0x12, 0xab, 0xcd, 0x89,
                0x71, 0xab, 0x12, 0x89, 0xcd,
                0x54, 0x12,
                0x54, 0xab,
                0x81, 0x12, 0xab, 0xcd, 0x89, 0x12, 0xab, 0xcd, 0x89,
                0x81, 0xab, 0x12, 0x89, 0xcd, 0xab, 0x12, 0x89, 0xcd,
                0x55, 0x12,
                0x55, 0xab,
                0x72, 0x12, 0xab, 0xcd, 0x89,
                0x72, 0xab, 0x12, 0x89, 0xcd,
                0x82, 0x12, 0xab, 0xcd, 0x89, 0x12, 0xab, 0xcd, 0x89,
                0x82, 0xab, 0x12, 0x89, 0xcd, 0xab, 0x12, 0x89, 0xcd,
                0x73, 0x00, 0x00, 0x00, 0x51,
                0x83, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0xe8,
                0x83, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfc, 0x18,
                0x98, 0x57, 0xad, 0x04, 0xe0, 0x99, 0x94, 0x4a, 0x2d, 0xac, 0xd6, 0x64, 0x61, 0xa5, 0x19, 0xcd, 0xb9
            };

#if NETMF
            DateTime dt1 = new DateTime(116444736000000000 + 10000000, DateTimeKind.Utc); // 1970, 1, 1, 0, 0, 1;
            DateTime dt2 = new DateTime(116444736000000000 - 10000000, DateTimeKind.Utc); // 1969, 12, 31, 23, 59, 59;
#else
            DateTime dt1 = new DateTime(1970, 1, 1, 0, 0, 1, DateTimeKind.Utc);
            DateTime dt2 = new DateTime(1969, 12, 31, 23, 59, 59, DateTimeKind.Utc);
#endif

#if COMPACT_FRAMEWORK
            Guid uuid = new Guid(0x57ad04e0, unchecked((short)0x9994), 0x4a2d, 0xac, 0xd6, 0x64, 0x61, 0xa5, 0x19, 0xcd, 0xb9);
#else
            Guid uuid = new Guid(0x57ad04e0, 0x9994, 0x4a2d, 0xac, 0xd6, 0x64, 0x61, 0xa5, 0x19, 0xcd, 0xb9);
#endif

            ByteBuffer buffer = new ByteBuffer(payload.Length, false);

#if SMALL_MEMORY
            Encoder.WriteObject(ref buffer, null);
            Encoder.WriteBoolean(ref buffer, true, false);
            Encoder.WriteBoolean(ref buffer, false, false);
            Encoder.WriteBoolean(ref buffer, true, true);
            Encoder.WriteBoolean(ref buffer, false, true);
            Encoder.WriteUByte(ref buffer, 0x12);
            Encoder.WriteUByte(ref buffer, 0xab);
            Encoder.WriteUShort(ref buffer, 0x12ab);
            Encoder.WriteUShort(ref buffer, 0xab12);
            Encoder.WriteUInt(ref buffer, (uint)0x12abcd89, false);
            Encoder.WriteUInt(ref buffer, (uint)0xab1289cd, false);
            Encoder.WriteUInt(ref buffer, (uint)0x12, true);
            Encoder.WriteUInt(ref buffer, (uint)0xab, true);
            Encoder.WriteUInt(ref buffer, (uint)0, true);
            Encoder.WriteULong(ref buffer, (ulong)0x12abcd8912abcd89, false);
            Encoder.WriteULong(ref buffer, (ulong)0xab1289cdab1289cd, false);
            Encoder.WriteULong(ref buffer, (ulong)0x12, true);
            Encoder.WriteULong(ref buffer, (ulong)0xab, true);
            Encoder.WriteULong(ref buffer, (ulong)0, true);
            Encoder.WriteByte(ref buffer, 0x12);
            Encoder.WriteByte(ref buffer, unchecked((sbyte)0xab));
            Encoder.WriteShort(ref buffer, 0x12ab);
            Encoder.WriteShort(ref buffer, unchecked((short)0xab12));
            Encoder.WriteInt(ref buffer, 0x12abcd89, false);
            Encoder.WriteInt(ref buffer, unchecked((int)0xab1289cd), false);
            Encoder.WriteInt(ref buffer, 0x12, true);
            Encoder.WriteInt(ref buffer, unchecked((sbyte)0xab), true);
            Encoder.WriteLong(ref buffer, 0x12abcd8912abcd89, false);
            Encoder.WriteLong(ref buffer, unchecked((long)0xab1289cdab1289cd), false);
            Encoder.WriteLong(ref buffer, 0x12, true);
            Encoder.WriteLong(ref buffer, unchecked((sbyte)0xab), true);
            Encoder.WriteFloat(ref buffer, 1.08422855E-27F);
            Encoder.WriteFloat(ref buffer, -5.20608567E-13F);
            Encoder.WriteDouble(ref buffer, 9.8451612575257768E-219);
            Encoder.WriteDouble(ref buffer, -3.3107870105667015E-101);
            Encoder.WriteChar(ref buffer, 'Q');
            Encoder.WriteTimestamp(ref buffer, dt1);
            Encoder.WriteTimestamp(ref buffer, dt2);
            Encoder.WriteUuid(ref buffer, uuid);

            // FIXME
            //Fx.Assert(payload.Length == buffer.Length, "size not equal");
            //for (int i = 0; i < payload.Length; i++)
            //{
            //    Fx.Assert(payload[i] == buffer.Buffer[i], Fx.Format("the {0} byte is different: {1} <-> {2}", i, payload[i], buffer.Buffer[i]));
            //}

            //Fx.Assert(Encoder.ReadObject(ref buffer) == null, "null");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals(true), "true");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals(false), "false");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals(true), "true");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals(false), "false");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((byte)0x12), "byte1");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((byte)0xab), "byte2");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((ushort)0x12ab), "ushort1");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((ushort)0xab12), "ushort2");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((uint)0x12abcd89), "uint1");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((uint)0xab1289cd), "uint2");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((uint)0x12), "uint3");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((uint)0xab), "uint4");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((uint)0), "uint0");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((ulong)0x12abcd8912abcd89), "ulong1");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((ulong)0xab1289cdab1289cd), "ulong2");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((ulong)0x12), "ulong3");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((ulong)0xab), "ulong4");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((ulong)0), "ulong0");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((sbyte)0x12), "sbyte1");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals(unchecked((sbyte)0xab)), "sbyte2");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((short)0x12ab), "short1");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals(unchecked((short)0xab12)), "short2");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((int)0x12abcd89), "int1");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals(unchecked((int)0xab1289cd)), "int2");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((int)0x12), "int3");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((int)unchecked((sbyte)0xab)), "int4");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((long)0x12abcd8912abcd89), "long1");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals(unchecked((long)0xab1289cdab1289cd)), "long2");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((long)0x12), "long3");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals((long)unchecked((sbyte)0xab)), "long4");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals(1.08422855E-27F), "float1");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals(-5.20608567E-13F), "flaot2");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals(9.8451612575257768E-219), "double1");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals(-3.3107870105667015E-101), "double2");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals('Q'), "char");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals(dt1), "timestamp1");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals(dt2), "timestamp2");
            //Fx.Assert(Encoder.ReadObject(ref buffer).Equals(uuid), "uuid");
#else
            Encoder.WriteObject(buffer, null);
            Encoder.WriteBoolean(buffer, true, false);
            Encoder.WriteBoolean(buffer, false, false);
            Encoder.WriteBoolean(buffer, true, true);
            Encoder.WriteBoolean(buffer, false, true);
            Encoder.WriteUByte(buffer, 0x12);
            Encoder.WriteUByte(buffer, 0xab);
            Encoder.WriteUShort(buffer, 0x12ab);
            Encoder.WriteUShort(buffer, 0xab12);
            Encoder.WriteUInt(buffer, (uint)0x12abcd89, false);
            Encoder.WriteUInt(buffer, (uint)0xab1289cd, false);
            Encoder.WriteUInt(buffer, (uint)0x12, true);
            Encoder.WriteUInt(buffer, (uint)0xab, true);
            Encoder.WriteUInt(buffer, (uint)0, true);
            Encoder.WriteULong(buffer, (ulong)0x12abcd8912abcd89, false);
            Encoder.WriteULong(buffer, (ulong)0xab1289cdab1289cd, false);
            Encoder.WriteULong(buffer, (ulong)0x12, true);
            Encoder.WriteULong(buffer, (ulong)0xab, true);
            Encoder.WriteULong(buffer, (ulong)0, true);
            Encoder.WriteByte(buffer, 0x12);
            Encoder.WriteByte(buffer, unchecked((sbyte)0xab));
            Encoder.WriteShort(buffer, 0x12ab);
            Encoder.WriteShort(buffer, unchecked((short)0xab12));
            Encoder.WriteInt(buffer, 0x12abcd89, false);
            Encoder.WriteInt(buffer, unchecked((int)0xab1289cd), false);
            Encoder.WriteInt(buffer, 0x12, true);
            Encoder.WriteInt(buffer, unchecked((sbyte)0xab), true);
            Encoder.WriteLong(buffer, 0x12abcd8912abcd89, false);
            Encoder.WriteLong(buffer, unchecked((long)0xab1289cdab1289cd), false);
            Encoder.WriteLong(buffer, 0x12, true);
            Encoder.WriteLong(buffer, unchecked((sbyte)0xab), true);
            Encoder.WriteFloat(buffer, 1.08422855E-27F);
            Encoder.WriteFloat(buffer, -5.20608567E-13F);
            Encoder.WriteDouble(buffer, 9.8451612575257768E-219);
            Encoder.WriteDouble(buffer, -3.3107870105667015E-101);
            Encoder.WriteChar(buffer, 'Q');
            Encoder.WriteTimestamp(buffer, dt1);
            Encoder.WriteTimestamp(buffer, dt2);
            Encoder.WriteUuid(buffer, uuid);

            Fx.Assert(payload.Length == buffer.Length, "size not equal");
            for (int i = 0; i < payload.Length; i++)
            {
                Fx.Assert(payload[i] == buffer.Buffer[i], Fx.Format("the {0} byte is different: {1} <-> {2}", i, payload[i], buffer.Buffer[i]));
            }

            Fx.Assert(Encoder.ReadObject(buffer) == null, "null");
            Fx.Assert(Encoder.ReadObject(buffer).Equals(true), "true");
            Fx.Assert(Encoder.ReadObject(buffer).Equals(false), "false");
            Fx.Assert(Encoder.ReadObject(buffer).Equals(true), "true");
            Fx.Assert(Encoder.ReadObject(buffer).Equals(false), "false");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((byte)0x12), "byte1");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((byte)0xab), "byte2");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((ushort)0x12ab), "ushort1");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((ushort)0xab12), "ushort2");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((uint)0x12abcd89), "uint1");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((uint)0xab1289cd), "uint2");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((uint)0x12), "uint3");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((uint)0xab), "uint4");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((uint)0), "uint0");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((ulong)0x12abcd8912abcd89), "ulong1");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((ulong)0xab1289cdab1289cd), "ulong2");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((ulong)0x12), "ulong3");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((ulong)0xab), "ulong4");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((ulong)0), "ulong0");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((sbyte)0x12), "sbyte1");
            Fx.Assert(Encoder.ReadObject(buffer).Equals(unchecked((sbyte)0xab)), "sbyte2");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((short)0x12ab), "short1");
            Fx.Assert(Encoder.ReadObject(buffer).Equals(unchecked((short)0xab12)), "short2");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((int)0x12abcd89), "int1");
            Fx.Assert(Encoder.ReadObject(buffer).Equals(unchecked((int)0xab1289cd)), "int2");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((int)0x12), "int3");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((int)unchecked((sbyte)0xab)), "int4");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((long)0x12abcd8912abcd89), "long1");
            Fx.Assert(Encoder.ReadObject(buffer).Equals(unchecked((long)0xab1289cdab1289cd)), "long2");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((long)0x12), "long3");
            Fx.Assert(Encoder.ReadObject(buffer).Equals((long)unchecked((sbyte)0xab)), "long4");
            Fx.Assert(Encoder.ReadObject(buffer).Equals(1.08422855E-27F), "float1");
            Fx.Assert(Encoder.ReadObject(buffer).Equals(-5.20608567E-13F), "flaot2");
            Fx.Assert(Encoder.ReadObject(buffer).Equals(9.8451612575257768E-219), "double1");
            Fx.Assert(Encoder.ReadObject(buffer).Equals(-3.3107870105667015E-101), "double2");
            Fx.Assert(Encoder.ReadObject(buffer).Equals('Q'), "char");
            Fx.Assert(Encoder.ReadObject(buffer).Equals(dt1), "timestamp1");
            Fx.Assert(Encoder.ReadObject(buffer).Equals(dt2), "timestamp2");
            Fx.Assert(Encoder.ReadObject(buffer).Equals(uuid), "uuid");
#endif

        }
    }
}
