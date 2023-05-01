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
    using System;
    using System.Collections;
    using System.Reflection;
    using global::Amqp;
    using global::Amqp.Framing;
    using global::Amqp.Types;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Decimal = global::Amqp.Types.Decimal;

    [TestClass]
    public class AmqpCodecTests
    {
        bool boolTrue = true;
        byte[] boolTrueBin = new byte[] { 0x41 };
        byte[] boolTrueBin1 = new byte[] { 0x56, 0x01 };

        bool boolFalse = false;
        byte[] boolFalseBin = new byte[] { 0x42 };
        byte[] boolFalseBin1 = new byte[] { 0x56, 0x00 };

        byte ubyteValue = 0x33;
        byte[] ubyteValueBin = new byte[] { 0x50, 0x33 };

        ushort ushortValue = 0x1234;
        byte[] ushortValueBin = new byte[] { 0x60,  0x12, 0x34};

        uint uint0Value = 0x00;
        byte[] uint0ValueBin = new byte[] { 0x43 };

        uint uintSmallValue = 0xe1;
        byte[] uintSmallValueBin = new byte[] { 0x52, 0xe1 };

        uint uintValue = 0xedcba098;
        byte[] uintValueBin = new byte[] { 0x70, 0xed, 0xcb, 0xa0, 0x98 };

        ulong ulong0Value = 0x00;
        byte[] ulong0ValueBin = new byte[] { 0x44 };

        ulong ulongSmallValue = 0xf2;
        byte[] ulongSmallValueBin = new byte[] { 0x53, 0xf2 };

        ulong ulongValue = 0x12345678edcba098;
        byte[] ulongValueBin = new byte[] { 0x80, 0x12, 0x34, 0x56, 0x78, 0xed, 0xcb, 0xa0, 0x98 };

        sbyte byteValue = -20;
        byte[] byteValueBin = new byte[] { 0x51, 0xec };

        short shortValue = 0x5678;
        byte[] shortValueBin = new byte[] { 0x61, 0x56, 0x78 };

        int intSmallValue = -77;
        byte[] intSmallValueBin = new byte[] { 0x54, 0xb3 };

        int intValue = 0x56789a00;
        byte[] intValueBin = new byte[] { 0x71, 0x56, 0x78, 0x9a, 0x00 };

        long longSmallValue = 0x22;
        byte[] longSmallValueBin = new byte[] { 0x55, 0x22 };

        long longValue = -111111111111; //FFFFFFE62142FE39
        byte[] longValueBin = new byte[] { 0x81, 0xff, 0xff, 0xff, 0xe6, 0x21, 0x42, 0xfe, 0x39 };

        float floatValue = -88.88f;
        byte[] floatValueBin = new byte[] { 0x72, 0xc2, 0xb1, 0xc2, 0x8f };

        double doubleValue = 111111111111111.22222222222;
        byte[] doubleValueBin = new byte[] { 0x82, 0x42, 0xd9, 0x43, 0x84, 0x93, 0xbc, 0x71, 0xce };

        byte[] decimal32ValueBin = new byte[] { 0x74, 0x30, 0x92, 0xd6, 0x87 };
        Decimal decimal32Value = new Decimal(new byte[] { 0x30, 0x92, 0xd6, 0x87 });

        byte[] decimal64ValueBin = new byte[] { 0x84, 0xb1, 0x04, 0x62, 0xd5, 0x3d, 0x21, 0x6e, 0xf4 };
        Decimal decimal64Value = new Decimal(new byte[] { 0xb1, 0x04, 0x62, 0xd5, 0x3d, 0x21, 0x6e, 0xf4 });

        byte[] decimal128ValueBin = new byte[] { 0x94, 0x30, 0x40, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
        Decimal decimal128Value = new Decimal(new byte[] { 0x30, 0x40, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff });

        char charValue = 'A';
        byte[] charValueBin = new byte[] { 0x73, 0x00, 0x00, 0x00, 0x41 };

        DateTime dtValue = DateTime.Parse("2008-11-01T19:35:00.0000000Z").ToUniversalTime();
        byte[] dtValueBin = new byte[] { 0x83, 0x00, 0x00, 0x01, 0x1d, 0x59, 0x8d, 0x1e, 0xa0 };

        Guid uuidValue = new Guid("f275ea5e-0c57-4ad7-b11a-b20c563d3b71");
        byte[] uuidValueBin = new byte[] { 0x98, 0xf2, 0x75, 0xea, 0x5e, 0x0c, 0x57, 0x4a, 0xd7, 0xb1, 0x1a, 0xb2, 0x0c, 0x56, 0x3d, 0x3b, 0x71 };

        byte[] bin8Value = new byte[56];
        byte[] bin32Value = new byte[500];
        byte[] bin8ValueBin = new byte[1 + 1 + 56];
        byte[] bin32ValueBin = new byte[1 + 4 + 500];

        string strValue = "amqp";
        byte[] str8Utf8ValueBin = new byte[] { 0xa1, 0x04, 0x61, 0x6d, 0x71, 0x70 };
        byte[] str32Utf8ValueBin = new byte[] { 0xb1, 0x00, 0x00, 0x00, 0x04, 0x61, 0x6d, 0x71, 0x70 };
        byte[] sym8ValueBin = new byte[] { 0xa3, 0x04, 0x61, 0x6d, 0x71, 0x70 };
        byte[] sym32ValueBin = new byte[] { 0xb3, 0x00, 0x00, 0x00, 0x04, 0x61, 0x6d, 0x71, 0x70 };

        DescribedValue described1 = CreateDescribed(100, null, "value1");
        DescribedValue described2 = CreateDescribed(0, "v2", (float)3.14159);
        DescribedValue described3 = CreateDescribed(0, "v3", Guid.NewGuid());
        DescribedValue described4 = CreateDescribed(ulong.MaxValue, null, new List() { 100, "200" });
        DescribedValue described5 = CreateDescribed(12345L, "", new string[] { "string1", "string2", "string3", "string4" });

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestInitialize]
        public void TestInitialize()
        {
        }

        public AmqpCodecTests()
        {
            Random random = new Random();
            for (int i = 0; i < bin8Value.Length; i++) bin8Value[i] = (byte)random.Next(255);
            for (int i = 0; i < bin32Value.Length; i++) bin32Value[i] = (byte)random.Next(255);
            bin8ValueBin[0] = 0xa0;
            bin8ValueBin[1] = 56;
            bin32ValueBin[0] = 0xb0;
            bin32ValueBin[1] = 0x00;
            bin32ValueBin[2] = 0x00;
            bin32ValueBin[3] = 0x01;
            bin32ValueBin[4] = 0xf4;
            Buffer.BlockCopy(bin8Value, 0, bin8ValueBin, 2, bin8Value.Length);
            Buffer.BlockCopy(bin32Value, 0, bin32ValueBin, 5, bin32Value.Length);
        }

        [TestMethod()]
        public void AmqpCodecSingleValueTest()
        {
            byte[] workBuffer = new byte[2048];

            RunSingleValueTest(workBuffer, boolTrue, boolTrueBin, "Boolean value is not true.");
            RunSingleValueTest(workBuffer, boolFalse, boolFalseBin, "Boolean value is not false.");
            RunSingleValueTest(workBuffer, ubyteValue, ubyteValueBin, "UByte value is not equal.");
            RunSingleValueTest(workBuffer, ushortValue, ushortValueBin, "UShort value is not equal.");
            RunSingleValueTest(workBuffer, uint0Value, uint0ValueBin, "UInt0 value is not equal.");
            RunSingleValueTest(workBuffer, uintSmallValue, uintSmallValueBin, "UIntSmall value is not equal.");
            RunSingleValueTest(workBuffer, uintValue, uintValueBin, "UInt value is not equal.");
            RunSingleValueTest(workBuffer, ulong0Value, ulong0ValueBin, "ULong0 value is not equal.");
            RunSingleValueTest(workBuffer, ulongSmallValue, ulongSmallValueBin, "ULongSmall value is not equal.");
            RunSingleValueTest(workBuffer, ulongValue, ulongValueBin, "ULong value is not equal.");
            RunSingleValueTest(workBuffer, byteValue, byteValueBin, "Byte value is not equal.");
            RunSingleValueTest(workBuffer, shortValue, shortValueBin, "Short value is not equal.");
            RunSingleValueTest(workBuffer, intSmallValue, intSmallValueBin, "Int small value is not equal.");
            RunSingleValueTest(workBuffer, intValue, intValueBin, "Int value is not equal.");
            RunSingleValueTest(workBuffer, longSmallValue, longSmallValueBin, "Long small value is not equal.");
            RunSingleValueTest(workBuffer, longValue, longValueBin, "Long value is not equal.");
            RunSingleValueTest(workBuffer, floatValue, floatValueBin, "Float value is not equal.");
            RunSingleValueTest(workBuffer, doubleValue, doubleValueBin, "Double value is not equal.");
            RunSingleValueTest(workBuffer, decimal32Value, decimal32ValueBin, "Decimal32 value is not equal.");
            RunSingleValueTest(workBuffer, decimal64Value, decimal64ValueBin, "Decimal64 value is not equal.");
            RunSingleValueTest(workBuffer, decimal128Value, decimal128ValueBin, "Decimal128 value is not equal.");
            RunSingleValueTest(workBuffer, charValue, charValueBin, "Char value is not equal.");
            RunSingleValueTest(workBuffer, dtValue, dtValueBin, "Timestamp value is not equal.");
            RunSingleValueTest(workBuffer, uuidValue, uuidValueBin, "Uuid value is not equal.");
            RunSingleValueTest(workBuffer, bin8Value, bin8ValueBin, "Binary8 value is not equal.");
            RunSingleValueTest(workBuffer, bin32Value, bin32ValueBin, "Binary32 value is not equal.");
            RunSingleValueTest(workBuffer, (Symbol)strValue, sym8ValueBin, "Symbol8 string value is not equal.");
            RunSingleValueTest(workBuffer, strValue, str8Utf8ValueBin, "UTF8 string8 string value is not equal.");

            // symbol 32
            Symbol symbol32v = (Symbol)Encoder.ReadObject(new ByteBuffer(sym32ValueBin, 0, sym32ValueBin.Length, sym32ValueBin.Length));
            Assert.IsTrue((string)symbol32v == strValue, "Symbol32 string value is not equal.");

            // string 32 UTF8
            string str32Utf8 = (string)Encoder.ReadObject(new ByteBuffer(str32Utf8ValueBin, 0, str32Utf8ValueBin.Length, str32Utf8ValueBin.Length));
            Assert.IsTrue(str32Utf8 == strValue, "UTF8 string32 string value is not equal.");
        }

        [TestMethod()]
        public void AmqpCodecListTest()
        {
            byte[] workBuffer = new byte[4096];
            ByteBuffer buffer = new ByteBuffer(workBuffer, 0, 0, workBuffer.Length);
            string strBig = new string('A', 512);

            List list = new List();
            list.Add(boolTrue);
            list.Add(boolFalse);
            list.Add(ubyteValue);
            list.Add(ushortValue);
            list.Add(uintValue);
            list.Add(ulongValue);
            list.Add(byteValue);
            list.Add(shortValue);
            list.Add(intValue);
            list.Add(longValue);
            list.Add(null);
            list.Add(floatValue);
            list.Add(doubleValue);
            list.Add(charValue);
            list.Add(dtValue);
            list.Add(uuidValue);
            list.Add(bin8ValueBin);
            list.Add(bin32ValueBin);
            list.Add((Symbol)null);
            list.Add(new Symbol(strValue));
            list.Add(new Symbol(strBig));
            list.Add(strValue);
            list.Add(strBig);
            list.Add(described1);
            list.Add(described2);
            list.Add(described3);
            list.Add(described4);

            Encoder.WriteObject(buffer, list);

            // make sure the size written is correct (it has to be List32)
            // the first byte is FormatCode.List32
            int listSize = (workBuffer[1] << 24) | (workBuffer[2] << 16) | (workBuffer[3] << 8) | workBuffer[4];
            Assert.AreEqual(buffer.Length - 5, listSize);

            IList decList = (IList)Encoder.ReadObject(buffer);
            int index = 0;

            Assert.IsTrue(decList[index++].Equals(true), "Boolean true expected.");
            Assert.IsTrue(decList[index++].Equals(false), "Boolean false expected.");
            Assert.IsTrue(decList[index++].Equals(ubyteValue), "UByte value not equal.");
            Assert.IsTrue(decList[index++].Equals(ushortValue), "UShort value not equal.");
            Assert.IsTrue(decList[index++].Equals(uintValue), "UInt value not equal.");
            Assert.IsTrue(decList[index++].Equals(ulongValue), "ULong value not equal.");
            Assert.IsTrue(decList[index++].Equals(byteValue), "Byte value not equal.");
            Assert.IsTrue(decList[index++].Equals(shortValue), "Short value not equal.");
            Assert.IsTrue(decList[index++].Equals(intValue), "Int value not equal.");
            Assert.IsTrue(decList[index++].Equals(longValue), "Long value not equal.");
            Assert.IsTrue(decList[index++] == null, "Null object expected.");
            Assert.IsTrue(decList[index++].Equals(floatValue), "Float value not equal.");
            Assert.IsTrue(decList[index++].Equals(doubleValue), "Double value not equal.");
            Assert.IsTrue(decList[index++].Equals(charValue), "Char value not equal.");
            Assert.IsTrue(decList[index++].Equals(dtValue), "TimeStamp value not equal.");
            Assert.IsTrue(decList[index++].Equals(uuidValue), "Uuid value not equal.");

            byte[] bin8 = (byte[])decList[index++];
            EnsureEqual(bin8, 0, bin8.Length, bin8ValueBin, 0, bin8ValueBin.Length);
            byte[] bin32 = (byte[])decList[index++];
            EnsureEqual(bin32, 0, bin32.Length, bin32ValueBin, 0, bin32ValueBin.Length);

            Assert.IsTrue(decList[index++] == null, "Null symbol expected.");
            Symbol symDecode = (Symbol)decList[index++];
            Assert.IsTrue(symDecode.Equals((Symbol)strValue), "AmqpSymbol value not equal.");
            symDecode = (Symbol)decList[index++];
            Assert.IsTrue(symDecode.Equals((Symbol)strBig), "AmqpSymbol value (big) not equal.");

            string strDecode = (string)decList[index++];
            Assert.IsTrue(strDecode.Equals(strValue), "string value not equal.");
            strDecode = (string)decList[index++];
            Assert.IsTrue(strDecode.Equals(strBig), "string value (big) not equal.");

            DescribedValue described = (DescribedValue)decList[index++];
            Assert.IsTrue(described.Descriptor.Equals(described1.Descriptor), "Described value 1 descriptor is different");
            Assert.IsTrue(described.Value.Equals(described1.Value), "Described value 1 value is different");
            described = (DescribedValue)decList[index++];
            Assert.IsTrue(described.Descriptor.Equals(described2.Descriptor), "Described value 2 descriptor is different");
            Assert.IsTrue(described.Value.Equals(described2.Value), "Described value 2 value is different");
            described = (DescribedValue)decList[index++];
            Assert.IsTrue(described.Descriptor.Equals(described3.Descriptor), "Described value 3 descriptor is different");
            Assert.IsTrue(described.Value.Equals(described3.Value), "Described value 3 value is different");
            described = (DescribedValue)decList[index++];
            Assert.IsTrue(described.Descriptor.Equals(described4.Descriptor), "Described value 4 descriptor is different");
            EnsureEqual((IList)described.Value, (IList)described4.Value);
        }

        [TestMethod()]
        public void AmqpCodecTimestampRangeTest()
        {
            long max = (long)(DateTime.MaxValue - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            long min = (long)(DateTime.MinValue - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            Assert.AreEqual(DateTime.MaxValue, Encoder.TimestampToDateTime(max));
            Assert.AreEqual(DateTime.MaxValue, Encoder.TimestampToDateTime(max + 1));
            Assert.AreEqual(DateTime.MinValue, Encoder.TimestampToDateTime(min));
            Assert.AreEqual(DateTime.MinValue, Encoder.TimestampToDateTime(min - 1));
        }

        [TestMethod()]
        public void AmqpCodecList0Test()
        {
            byte[] list0Bin = new byte[] { 0x45 };
            byte[] workBuffer = new byte[128];
            ByteBuffer buffer = new ByteBuffer(workBuffer, 0, 0, workBuffer.Length);

            List list0 = new List();
            Encoder.WriteObject(buffer, list0);
            EnsureEqual(list0Bin, 0, list0Bin.Length, buffer.Buffer, buffer.Offset, buffer.Length);

            IList list0v = (IList)Encoder.ReadObject(buffer);
            Assert.IsTrue(list0v.Count == 0, "The list should contain 0 items.");
        }

        [TestMethod()]
        public void AmqpCodecArrayTest()
        {
            byte[] workBuffer = new byte[4096];

            RunArrayTest<int>(workBuffer, i => i, 100);
            RunArrayTest<string>(workBuffer, i => "string" + i, 16);
        }

        [TestMethod()]
        public void AmqpCodecMapTest()
        {
            byte[] workBuffer = new byte[4096];
            ByteBuffer buffer = new ByteBuffer(workBuffer, 0, 0, workBuffer.Length);
            string strBig = new string('A', 512);

            Map map = new Map();
            map.Add(new Symbol("boolTrue"), boolTrue);
            map.Add(new Symbol("boolFalse"), boolFalse);
            map.Add(new Symbol("ubyte"), ubyteValue);
            map.Add(new Symbol("ushort"), ushortValue);
            map.Add(new Symbol("uint"), uintValue);
            map.Add(new Symbol("ulong"), ulongValue);
            map.Add(new Symbol("byte"), byteValue);
            map.Add(new Symbol("short"), shortValue);
            map.Add(new Symbol("int"), intValue);
            map.Add(new Symbol("long"), longValue);
            map.Add(new Symbol("null"), null);
            map.Add(new Symbol("float"), floatValue);
            map.Add(new Symbol("double"), doubleValue);
            map.Add(new Symbol("char"), charValue);
            map.Add(new Symbol("datetime"), dtValue);
            map.Add(new Symbol("uuid"), uuidValue);
            map.Add(new Symbol("binaryNull"), null);
            map.Add(new Symbol("binary8"), bin8ValueBin);
            map.Add(new Symbol("binary32"), bin32ValueBin);
            map.Add(new Symbol("symbolNull"), (Symbol)null);
            map.Add(new Symbol("symbol8"), new Symbol(strValue));
            map.Add(new Symbol("symbol32"), new Symbol(strBig));
            map.Add(new Symbol("string8"), strValue);
            map.Add(new Symbol("string32"), strBig);
            map.Add(new Symbol("described1"), described1);

            Encoder.WriteObject(buffer, map);

            // make sure the size written is correct (it has to be Map32)
            // the first byte is FormatCode.Map32
            int mapSize = (workBuffer[1] << 24) | (workBuffer[2] << 16) | (workBuffer[3] << 8) | workBuffer[4];
            Assert.AreEqual(buffer.Length - 5, mapSize);

            Map decMap = (Map)Encoder.ReadObject(buffer);

            Assert.IsTrue(decMap[new Symbol("boolTrue")].Equals(true), "Boolean true expected.");
            Assert.IsTrue(decMap[new Symbol("boolFalse")].Equals(false), "Boolean false expected.");
            Assert.IsTrue(decMap[new Symbol("ubyte")].Equals(ubyteValue), "UByte value not equal.");
            Assert.IsTrue(decMap[new Symbol("ushort")].Equals(ushortValue), "UShort value not equal.");
            Assert.IsTrue(decMap[new Symbol("uint")].Equals(uintValue), "UInt value not equal.");
            Assert.IsTrue(decMap[new Symbol("ulong")].Equals(ulongValue), "ULong value not equal.");
            Assert.IsTrue(decMap[new Symbol("byte")].Equals(byteValue), "Byte value not equal.");
            Assert.IsTrue(decMap[new Symbol("short")].Equals(shortValue), "Short value not equal.");
            Assert.IsTrue(decMap[new Symbol("int")].Equals(intValue), "Int value not equal.");
            Assert.IsTrue(decMap[new Symbol("long")].Equals(longValue), "Long value not equal.");
            Assert.IsTrue(decMap[new Symbol("null")] == null, "Null object expected.");
            Assert.IsTrue(decMap[new Symbol("float")].Equals(floatValue), "Float value not equal.");
            Assert.IsTrue(decMap[new Symbol("double")].Equals(doubleValue), "Double value not equal.");
            Assert.IsTrue(decMap[new Symbol("char")].Equals(charValue), "Char value not equal.");
            Assert.IsTrue(decMap[new Symbol("datetime")].Equals(dtValue), "TimeStamp value not equal.");
            Assert.IsTrue(decMap[new Symbol("uuid")].Equals(uuidValue), "Uuid value not equal.");
            Assert.IsTrue(decMap[new Symbol("binaryNull")] == null, "Null binary expected.");
            byte[] bin8 = (byte[])decMap[new Symbol("binary8")];
            EnsureEqual(bin8, 0, bin8.Length, bin8ValueBin, 0, bin8ValueBin.Length);
            byte[] bin32 = (byte[])decMap[new Symbol("binary32")];
            EnsureEqual(bin32, 0, bin32.Length, bin32ValueBin, 0, bin32ValueBin.Length);

            Assert.IsTrue(decMap[new Symbol("symbolNull")] == null, "Null symbol expected.");
            Symbol symDecode = (Symbol)decMap[new Symbol("symbol8")];
            Assert.IsTrue(symDecode.Equals((Symbol)strValue), "AmqpSymbol value not equal.");
            symDecode = (Symbol)decMap[new Symbol("symbol32")];
            Assert.IsTrue(symDecode.Equals((Symbol)strBig), "AmqpSymbol value (big) not equal.");

            string strDecode = (string)decMap[new Symbol("string8")];
            Assert.IsTrue(strDecode.Equals(strValue), "string value not equal.");
            strDecode = (string)decMap[new Symbol("string32")];
            Assert.IsTrue(strDecode.Equals(strBig), "string value (big) not equal.");

            DescribedValue described = (DescribedValue)decMap[new Symbol("described1")];
            Assert.IsTrue(described.Descriptor.Equals(described1.Descriptor), "Described value 1 descriptor is different");
            Assert.IsTrue(described.Value.Equals(described1.Value), "Described value 1 value is different");
        }

        [TestMethod()]
        public void AmqpCodecDescribedValueTest()
        {
            byte[] workBuffer = new byte[2048];

            Action<object, object, byte[]> runTest = (d, v, b) =>
            {
                var dv = new DescribedValue(d, v);
                ByteBuffer buffer;
                Encoder.WriteObject(buffer = new ByteBuffer(workBuffer, 0, 0, workBuffer.Length), dv);
                var dv2 = (DescribedValue)Encoder.ReadObject(buffer);

                Assert.AreEqual(dv.Descriptor, dv2.Descriptor);
                if (dv.Value == null)
                {
                    Assert.IsTrue(dv2.Value == null);
                }
                else if (dv.Value.GetType() == typeof(List))
                {
                    EnsureEqual((IList)dv.Value, (IList)dv2.Value);
                }
                else
                {
                    Assert.AreEqual(dv.Value, dv2.Value);
                }
            };

            runTest(0, "uri", workBuffer);
            runTest(long.MaxValue, (Symbol)"abcd", workBuffer);
            runTest("descriptor", new List() { 0, "x" }, workBuffer);
            runTest((Symbol)"null", null, workBuffer);
        }

        [TestMethod()]
        public void AmqpCodecFramingTypeTest()
        {
            Type codec = typeof(Open).Assembly.GetType("Amqp.Framing.Codec", true);
            var decode = codec.GetMethod("Decode", BindingFlags.Public | BindingFlags.Static);
            Random random = new Random();

            foreach (var type in codec.Assembly.GetTypes())
            {
                if (!typeof(RestrictedDescribed).IsAssignableFrom(type) ||
                    type.IsAbstract)
                {
                    continue;
                }

                if (type.GetConstructor(new Type[0]) == null)
                {
                    continue;
                }

                System.Diagnostics.Trace.WriteLine("testing " + type.FullName);

                object obj = CreateRestrictedDescribed(type, random);

                ByteBuffer buffer = new ByteBuffer(1024, true);
                ((RestrictedDescribed)obj).Encode(buffer);

                object obj2 = decode.Invoke(null, new object[] { buffer });
                ValidateRestrictedDescribed(obj, obj2);
            }
        }

        static DescribedValue CreateDescribed(ulong code, string symbol, object value)
        {
            return new DescribedValue(symbol == null ? (object)code : symbol, value);
        }

        static object CreateRestrictedDescribed(Type type, Random random)
        {
            object obj = Activator.CreateInstance(type);

            if (typeof(DescribedList).IsAssignableFrom(type))
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var p in props)
                {
                    if (p.PropertyType == typeof(object) ||
                        p.PropertyType.IsAbstract ||
                        p.GetSetMethod(false) == null)
                    {
                        continue;
                    }

                    bool simpleType = !typeof(RestrictedDescribed).IsAssignableFrom(p.PropertyType);
                    object value = simpleType ? 
                        CreateObject(p.PropertyType, true, true, random) : 
                        CreateRestrictedDescribed(p.PropertyType, random);
                    p.SetValue(obj, value, null);
                }
            }
            else if (typeof(DescribedMap).IsAssignableFrom(type))
            {
                Type keyType = null;
                Type temp = type;
                while (temp != null)
                {
                    FieldInfo fi = temp.GetField("keyType", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fi == null)
                    {
                        temp = temp.BaseType;
                    }
                    else
                    {
                        keyType = (Type)fi.GetValue(obj);
                        break;
                    }
                }

                Assert.IsTrue(keyType != null, "cannot find key type");
                var map = (DescribedMap)obj;
                for (int i = 0; i < 10; i++)
                {
                    object key = CreateObject(keyType, false, false, random);
                    object value = CreateObject(GetSingleValueType(false, random), true, false, random);
                    map[key] = value;
                }
            }
            else if (type == typeof(Data))
            {
                var data = (Data)obj;
                data.Binary = (byte[])CreateObject(typeof(byte[]), true, false, random);
            }
            else if (type == typeof(AmqpValue))
            {
                var value = (AmqpValue)obj;
                value.Value = CreateObject(GetSingleValueType(false, random), true, true, random);
            }
            else if (type == typeof(AmqpSequence))
            {
                var seq = (AmqpSequence)obj;
                seq.List = (object[])CreateObject(typeof(object[]), true, true, random);
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("dont know how to initialize " + type.Name);
            }

            return obj;
        }

        static void ValidateRestrictedDescribed(object x, object y)
        {
            Assert.AreEqual(x.GetType(), y.GetType());
            Assert.IsTrue(typeof(RestrictedDescribed).IsAssignableFrom(x.GetType()));

            if (typeof(DescribedList).IsAssignableFrom(x.GetType()))
            {
                var props = x.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var p in props)
                {
                    if (p.PropertyType == typeof(object) ||
                        p.PropertyType.IsAbstract ||
                        p.GetSetMethod(false) == null)
                    {
                        continue;
                    }

                    object v1 = p.GetValue(x, null);
                    object v2 = p.GetValue(y, null);
                    if (typeof(RestrictedDescribed).IsAssignableFrom(p.PropertyType))
                    {
                        ValidateRestrictedDescribed(v1, v2);
                    }
                    else
                    {
                        EnsureEqual(v1, v2);
                    }
                }
            }
            else if (typeof(DescribedMap).IsAssignableFrom(x.GetType()))
            {
                var v1 = (DescribedMap)x;
                var v2 = (DescribedMap)y;
                EnsureEqual(v1.Map, v2.Map);
            }
            else if (x.GetType() == typeof(Data))
            {
                EnsureEqual(((Data)x).Binary, ((Data)y).Binary);
            }
            else if (x.GetType() == typeof(DataList))
            {
                Assert.AreEqual(x, y);
            }
            else if (x.GetType() == typeof(AmqpValue))
            {
                EnsureEqual(((AmqpValue)x).Value, ((AmqpValue)y).Value);
            }
            else if (x.GetType() == typeof(AmqpSequence))
            {
                EnsureEqual(((AmqpSequence)x).List, ((AmqpSequence)y).List);
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("skip validation for " + x.GetType().Name);
            }
        }

        static Type GetSingleValueType(bool forKey, Random random)
        {
            Type[] types = new Type[]
            {
                typeof(bool),
                typeof(byte),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(sbyte),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(float),
                typeof(double),
                typeof(char),
                typeof(Guid),
                typeof(string),
                typeof(Symbol),
                typeof(DateTime),
                typeof(byte[])
            };

            return types[random.Next(types.Length - (forKey ? 2 : 0))];
        }

        static object CreateObject(Type type, bool allowNull, bool allowCollection, Random random)
        {
            if (allowNull && random.Next(10) < 3)
            {
                return null;
            }

            if (type == typeof(object))
            {
                type = GetSingleValueType(false, random);
            }

            if (type == typeof(bool))
            {
                return random.Next(2) == 1;
            }
            else if (type == typeof(byte))
            {
                return (byte)random.Next(byte.MaxValue + 1);
            }
            else if (type == typeof(ushort))
            {
                return (ushort)random.Next(ushort.MaxValue + 1);
            }
            else if (type == typeof(uint))
            {
                return (uint)random.Next() + (uint)random.Next();
            }
            else if (type == typeof(ulong))
            {
                return (ulong)random.Next() + (ulong)random.Next() + (ulong)random.Next() + (ulong)random.Next();
            }
            else if (type == typeof(sbyte))
            {
                return (sbyte)random.Next(byte.MaxValue + 1);
            }
            else if (type == typeof(short))
            {
                return (short)random.Next(ushort.MaxValue + 1);
            }
            else if (type == typeof(int))
            {
                return random.Next() * (random.Next(2) == 0 ? 1 : -1);
            }
            else if (type == typeof(long))
            {
                return ((long)random.Next() + (long)random.Next()) * (random.Next(2) == 0 ? 1 : -1);
            }
            else if (type == typeof(float))
            {
                return 3.14159F * (random.Next(2) == 0 ? 1 : -1);
            }
            else if (type == typeof(double))
            {
                return 1234567.9876 * (random.Next(2) == 0 ? 1 : -1);
            }
            else if (type == typeof(char))
            {
                return '&';
            }
            else if (type == typeof(DateTime))
            {
                return DateTime.UtcNow.AddSeconds(random.Next(2000) * (random.Next(2) == 0 ? 1 : -1));
            }
            else if (type == typeof(Guid))
            {
                return Guid.NewGuid();
            }
            else if (type == typeof(byte[]))
            {
                byte[] b = new byte[random.Next(300)];
                for (int i = 0; i < b.Length; i++) b[i] = (byte)random.Next(byte.MaxValue + 1);
                return b;
            }
            else if (type == typeof(string))
            {
                return new string('A', random.Next(300));
            }
            else if (type == typeof(Symbol))
            {
                return new Symbol(new string('A', random.Next(300)));
            }
            else if (type == typeof(Fields))
            {
                Fields fields = new Fields();
                for (int i = 0; i < random.Next(10); i++)
                {
                    object key = CreateObject(typeof(Symbol), false, false, random);
                    fields[key] = CreateObject(GetSingleValueType(false, random), true, false, random);
                }

                return fields;
            }
            else if (type.IsEnum)
            {
                var values = Enum.GetValues(type);
                return values.GetValue(random.Next(values.Length));
            }
            else if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                Array array = Array.CreateInstance(elementType, random.Next(6) + 1);
                for (int i = 0; i < array.Length; i++)
                {
                    array.SetValue(CreateObject(elementType, false, false, random), i);
                }

                return array;
            }
            else if (allowCollection)
            {
                if (type == typeof(List))
                {
                    List list = new List();
                    for (int i = 0; i < random.Next(20); i++)
                    {
                        list.Add(CreateObject(GetSingleValueType(false, random), true, false, random));
                    }

                    return list;
                }
                else if (type == typeof(Map))
                {
                    Map map = new Map();
                    for (int i = 0; i < random.Next(10); i++)
                    {
                        object key = CreateObject(GetSingleValueType(true, random), false, false, random);
                        map[key] = CreateObject(GetSingleValueType(false, random), true, false, random);
                    }

                    return map;
                }
            }

            throw new Exception(type.Name + " is not covered!");
        }

        static void RunSingleValueTest<T>(byte[] workBuffer, T value, byte[] result, string message)
        {
            ByteBuffer buffer;
            Encoder.WriteObject(buffer = new ByteBuffer(workBuffer, 0, 0, workBuffer.Length), value);
            EnsureEqual(result, 0, result.Length, buffer.Buffer, buffer.Offset, buffer.Length);
            T decodeValue = (T)Encoder.ReadObject(new ByteBuffer(result, 0, result.Length, result.Length));
            if (typeof(T) == typeof(byte[]))
            {
                byte[] b1 = (byte[])(object)value;
                byte[] b2 = (byte[])(object)decodeValue;
                EnsureEqual(b1, 0, b1.Length, b2, 0, b2.Length);
            }
            else
            {
                Assert.IsTrue(value.Equals(decodeValue), message);
            }

            System.Diagnostics.Trace.WriteLine(typeof(T).Name + " test passed");
        }

        static void RunArrayTest<T>(byte[] workBuffer, Func<int, T> generator, int count)
        {
            ByteBuffer buffer = new ByteBuffer(workBuffer, 0, 0, workBuffer.Length);

            T[] array = new T[count];
            for (int i = 0; i < count; i++) array[i] = generator(i);
            Encoder.WriteObject(buffer, array);

            var array2 = (T[])Encoder.ReadObject(buffer);
            Assert.AreEqual(array.Length, array2.Length);
        }

        static void EnsureEqual(byte[] data1, int offset1, int count1, byte[] data2, int offset2, int count2)
        {
            Assert.IsTrue(count1 == count2, "Count is not equal.");
            for (int i = 0; i < count1; ++i)
            {
                byte b1 = data1[offset1 + i];
                byte b2 = data2[offset2 + i];
                Assert.IsTrue(b1 == b2, string.Format("The {0}th byte is not equal ({1} != {2}).", i, b1, b2));
            }
        }

        static void EnsureEqual(IList list1, IList list2)
        {
            if (list1 == null && list2 == null)
            {
                return;
            }

            Assert.IsTrue(list1 != null && list2 != null, "One of the list is null");

            Assert.IsTrue(list1.Count == list2.Count, "Count not equal.");
            for (int i = 0; i < list1.Count; i++)
            {
                EnsureEqual(list1[i], list2[i]);
            }
        }

        static void EnsureEqual(Map map1, Map map2)
        {
            if (map1 == null && map2 == null)
            {
                return;
            }

            Assert.IsTrue(map1 != null && map2 != null);
            Assert.AreEqual(map1.Count, map2.Count);

            foreach (var key in map1.Keys)
            {
                Assert.IsTrue(map2.ContainsKey(key));
                EnsureEqual(map1[key], map2[key]);
            }
        }

        static void EnsureEqual(DateTime d1, DateTime d2)
        {
            Assert.IsTrue(Math.Abs((d1.ToUniversalTime() - d2.ToUniversalTime()).TotalMilliseconds) < 5, "Datetime difference is greater than 5ms.");
        }

        static void EnsureEqual(object x, object y)
        {
            if (x == null && y == null)
            {
                return;
            }

            Assert.IsTrue(x != null && y != null);
            Assert.AreEqual(x.GetType(), y.GetType());

            if (x is IList)
            {
                EnsureEqual((IList)x, (IList)y);
            }
            else if (x is Map)
            {
                EnsureEqual((Map)x, (Map)y);
            }
            else if (x is byte[])
            {
                byte[] b1 = (byte[])x;
                byte[] b2 = (byte[])y;
                EnsureEqual(b1, 0, b1.Length, b2, 0, b2.Length);
            }
            else if (x is DateTime)
            {
                EnsureEqual((DateTime)x, (DateTime)y);
            }
            else
            {
                Assert.IsTrue(x.Equals(y));
            }
        }
    }
}
