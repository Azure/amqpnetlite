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
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using global::Amqp;
    using global::Amqp.Framing;
    using global::Amqp.Serialization;
    using global::Amqp.Types;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AmqpSerializerTests
    {
        [TestMethod()]
        public void AmqpSerializerPrimitiveTypeTest()
        {
            RunPrimitiveTypeTest<object>(null);

            RunPrimitiveTypeTest<bool>(true);
            RunPrimitiveTypeTest<bool>(false);
            
            RunPrimitiveTypeTest<byte>(byte.MinValue);
            RunPrimitiveTypeTest<byte>(222);
            RunPrimitiveTypeTest<byte>(byte.MaxValue);

            RunPrimitiveTypeTest<ushort>(ushort.MinValue);
            RunPrimitiveTypeTest<ushort>(2222);
            RunPrimitiveTypeTest<ushort>(ushort.MaxValue);

            RunPrimitiveTypeTest<uint>(uint.MinValue);
            RunPrimitiveTypeTest<uint>(22);
            RunPrimitiveTypeTest<uint>(2222222);
            RunPrimitiveTypeTest<uint>(uint.MaxValue);

            RunPrimitiveTypeTest<ulong>(ulong.MinValue);
            RunPrimitiveTypeTest<ulong>(22);
            RunPrimitiveTypeTest<ulong>(2222222222222);
            RunPrimitiveTypeTest<ulong>(ulong.MaxValue);

            RunPrimitiveTypeTest<sbyte>(sbyte.MinValue);
            RunPrimitiveTypeTest<sbyte>(-111);
            RunPrimitiveTypeTest<sbyte>(0);
            RunPrimitiveTypeTest<sbyte>(111);
            RunPrimitiveTypeTest<sbyte>(sbyte.MaxValue);

            RunPrimitiveTypeTest<short>(short.MinValue);
            RunPrimitiveTypeTest<short>(-11111);
            RunPrimitiveTypeTest<short>(0);
            RunPrimitiveTypeTest<short>(11111);
            RunPrimitiveTypeTest<short>(short.MaxValue);

            RunPrimitiveTypeTest<int>(int.MinValue);
            RunPrimitiveTypeTest<int>(-22);
            RunPrimitiveTypeTest<int>(0);
            RunPrimitiveTypeTest<int>(2222222);
            RunPrimitiveTypeTest<int>(int.MaxValue);

            RunPrimitiveTypeTest<long>(long.MinValue);
            RunPrimitiveTypeTest<long>(-222222222222);
            RunPrimitiveTypeTest<long>(22);
            RunPrimitiveTypeTest<long>(2222222222222);
            RunPrimitiveTypeTest<long>(long.MaxValue);

            RunPrimitiveTypeTest<float>(float.MinValue);
            RunPrimitiveTypeTest<float>(-123.456F);
            RunPrimitiveTypeTest<float>(0);
            RunPrimitiveTypeTest<float>(123.456F);
            RunPrimitiveTypeTest<float>(float.MaxValue);

            RunPrimitiveTypeTest<double>(double.MinValue);
            RunPrimitiveTypeTest<double>(-123.456789F);
            RunPrimitiveTypeTest<double>(0);
            RunPrimitiveTypeTest<double>(123.456789F);
            RunPrimitiveTypeTest<double>(double.MaxValue);

            RunPrimitiveTypeTest<char>('A');
            RunPrimitiveTypeTest<char>('中');

            RunPrimitiveTypeTest<DateTime>(DateTime.MinValue.ToUniversalTime());
            RunPrimitiveTypeTest<DateTime>(DateTime.UtcNow);
            RunPrimitiveTypeTest<DateTime>(DateTime.MaxValue.ToUniversalTime());

            RunPrimitiveTypeTest<Guid>(Guid.Empty);
            RunPrimitiveTypeTest<Guid>(Guid.NewGuid());

            RunPrimitiveTypeTest<byte[]>(new byte[0]);
            RunPrimitiveTypeTest<byte[]>(new byte[] { 4, 5, 6, 7, 8 });
            RunPrimitiveTypeTest<byte[]>(System.Text.Encoding.UTF8.GetBytes(new string('D', 888)));

            RunPrimitiveTypeTest<string>(string.Empty);
            RunPrimitiveTypeTest<string>("test");
            RunPrimitiveTypeTest<string>(new string('D', 888));

            RunPrimitiveTypeTest<Symbol>(string.Empty);
            RunPrimitiveTypeTest<Symbol>("test");
            RunPrimitiveTypeTest<Symbol>(new string('D', 888));

            RunPrimitiveTypeTest<Category>(Category.Food);
            RunPrimitiveTypeTest<Category?>(Category.Food);

            RunPrimitiveTypeTest<int?>(456);
            RunPrimitiveTypeTest<sbyte?>(-1);
            RunPrimitiveTypeTest<ulong?>(1234567890);
            RunPrimitiveTypeTest<byte?>(null);
        }

        static void RunPrimitiveTypeTest<T>(T value)
        {
            ByteBuffer b = new ByteBuffer(512, true);
            AmqpSerializer.Serialize(b, value);
            T o = AmqpSerializer.Deserialize<T>(b);

            if (typeof(T) == typeof(DateTime))
            {
                DateTime dt1 = (DateTime)(object)value;
                DateTime dt2 = (DateTime)(object)o;
                double diff = Math.Abs((dt2 - dt1).TotalMilliseconds);
                DateTime now = DateTime.UtcNow;
                long x = Convert.ToInt64((now - (DateTime)(object)value).TotalMilliseconds);
                long y = Convert.ToInt64((now - (DateTime)(object)o).TotalMilliseconds);
                Assert.IsTrue(diff < 2.0, string.Format(
                    "timestamp difference should be less than 2. ticks 1 {0} ticks 2 {1} diff {2}", dt1.Ticks, dt2.Ticks, diff));
            }
            else if (typeof(T) == typeof(byte[]))
            {
                byte[] b1 = (byte[])(object)value;
                byte[] b2 = (byte[])(object)o;
                Assert.AreEqual(b1.Length, b2.Length, "Count is not equal.");
                for (int i = 0; i < b1.Length; ++i)
                {
                    Assert.AreEqual(b1[i], b2[i], string.Format("The {0}th byte is not equal ({1} != {2}).", i, b1[i], b2[i]));
                }
            }
            else
            {
                Assert.AreEqual(value, o, "value not equal after deserialize");
            }
        }

        [TestMethod()]
        public void AmqpSerializerListEncodingTest()
        {
            Action<Person, Person> personValidator = (p1, p2) =>
            {
                Assert.IsTrue(p2 != null);
                Assert.AreEqual(21, p2.Age, "Age should be increased by OnDeserialized");
                Assert.AreEqual(p1.GetType().Name, p2.GetType().Name);
                Assert.AreEqual(p1.DateOfBirth.Value, p2.DateOfBirth.Value);
                Assert.AreEqual(p1.Properties.Count, p2.Properties.Count);
                foreach (var k in p1.Properties.Keys)
                {
                    Assert.AreEqual(p1.Properties[k], p2.Properties[k]);
                }
            };

            Action<List<int>, List<int>> gradesValidator = (l1, l2) =>
            {
                if (l1 == null || l2 == null)
                {
                    Assert.IsTrue(l1 == null && l2 == null);
                    return;
                }

                Assert.AreEqual(l1.Count, l2.Count);
                for (int i = 0; i < l1.Count; ++i)
                {
                    Assert.AreEqual(l1[i], l2[i]);
                }
            };

            // Create an object to be serialized
            Person p = new Student("Tom")
            {
                Address = new StreetAddress() { FullAddress = new string('B', 1024) },
                Grades = new List<int>() { 1, 2, 3, 4, 5 }
            };

            p.Age = 20;
            p.DateOfBirth = new DateTime(1980, 5, 12, 10, 2, 45, DateTimeKind.Utc);
            p.Properties.Add("height", 6.1);
            p.Properties.Add("male", true);
            p.Properties.Add("nick-name", "big foot");

            byte[] workBuffer = new byte[4096];
            ByteBuffer buffer = new ByteBuffer(workBuffer, 0, 0, workBuffer.Length);

            AmqpSerializer.Serialize(buffer, p);
            Assert.AreEqual(2, p.Version);

            // Deserialize and verify
            Person p3 = AmqpSerializer.Deserialize<Person>(buffer);
            Assert.AreEqual(2, p.Version);
            personValidator(p, p3);
            Assert.AreEqual(((Student)p).Address.FullAddress, ((Student)p3).Address.FullAddress);
            gradesValidator(((Student)p).Grades, ((Student)p3).Grades);

            // Inter-op: it should be an AMQP described list as other clients see it
            buffer.Seek(0);
            DescribedValue dl1 = AmqpSerializer.Deserialize<DescribedValue>(buffer);
            Assert.AreEqual(dl1.Descriptor, 0x0000123400000001UL);
            List lv = dl1.Value as List;
            Assert.IsTrue(lv != null);
            Assert.AreEqual(p.Name, lv[0]);
            Assert.AreEqual(p.Age, lv[1]);
            Assert.AreEqual(p.DateOfBirth.Value, lv[2]);
            Assert.IsTrue(lv[3] is DescribedValue, "Address is decribed type");
            Assert.AreEqual(((DescribedValue)lv[3]).Descriptor, 0x0000123400000003UL);
            Assert.AreEqual(((List)((DescribedValue)lv[3]).Value)[0], ((Student)p).Address.FullAddress);
            Assert.IsTrue(lv[4] is Map, "Properties should be map");
            Assert.AreEqual(((Map)lv[4])["height"], p.Properties["height"]);
            Assert.AreEqual(((Map)lv[4])["male"], p.Properties["male"]);
            Assert.AreEqual(((Map)lv[4])["nick-name"], p.Properties["nick-name"]);
            Assert.IsTrue(lv[5] is List);

            // Non-default serializer
            AmqpSerializer serializer = new AmqpSerializer();
            ByteBuffer bf1 = new ByteBuffer(1024, true);
            serializer.WriteObject(bf1, p);

            Person p4 = serializer.ReadObject<Person>(bf1);
            personValidator(p, p4);

            // Extensible: more items in the payload should not break
            DescribedValue dl2 = new DescribedValue(
                new Symbol("test.amqp:teacher"),
                new List() { "Jerry", 40, null, 50000, lv[4], null, null, "unknown-string", true, new Symbol("unknown-symbol") });
            ByteBuffer bf2 = new ByteBuffer(1024, true);
            serializer.WriteObject(bf2, dl2);
            serializer.WriteObject(bf2, 100ul);

            Person p5 = serializer.ReadObject<Person>(bf2);
            Assert.IsTrue(p5 is Teacher);
            Assert.IsTrue(p5.DateOfBirth == null);  // nullable should work
            Assert.AreEqual(100ul, serializer.ReadObject<object>(bf2));   // unknowns should be skipped
            Assert.AreEqual(0, bf2.Length);

            // teacher
            Teacher teacher = new Teacher("Han");
            teacher.Age = 30;
            teacher.Sallary = 60000;
            teacher.Classes = new Dictionary<int, string>() { { 101, "CS" }, { 102, "Math" }, { 205, "Project" } };

            ByteBuffer bf3 = new ByteBuffer(1024, true);
            serializer.WriteObject(bf3, teacher);

            Person p6 = serializer.ReadObject<Person>(bf3);
            Assert.IsTrue(p6 is Teacher);
            Assert.AreEqual(teacher.Age + 1, p6.Age);
            Assert.AreEqual(teacher.Sallary * 2, ((Teacher)p6).Sallary);
            Assert.AreEqual(teacher.Id, ((Teacher)p6).Id);
            Assert.AreEqual(teacher.Classes[101], ((Teacher)p6).Classes[101]);
            Assert.AreEqual(teacher.Classes[102], ((Teacher)p6).Classes[102]);
            Assert.AreEqual(teacher.Classes[205], ((Teacher)p6).Classes[205]);
        }

        [TestMethod()]
        public void AmqpSerializerMapEncodingTest()
        {
            // serializer test
            {
                var specification = new ComputerSpecification() {  Cores = 2, RamSize = 4, Description = "netbook" };
                var product = new Product() { Name = "Computer", Price = 499.98, Weight = 30, Specification = specification, Category = Category.Electronic };

                var buffer = new ByteBuffer(1024, true);
                AmqpSerializer.Serialize(buffer, product);
                Assert.AreEqual(product.Properties["OnSerializing"], "true");
                Assert.AreEqual(product.Properties["OnSerialized"], "true");

                var product2 = AmqpSerializer.Deserialize<Product>(buffer);
                Assert.AreEqual(product2.Properties["OnDeserializing"], "true");
                Assert.AreEqual(product2.Properties["OnDeserialized"], "true");
                Assert.AreEqual(product.Name, product2.Name);
                Assert.AreEqual(product.Price, product2.Price);
                Assert.AreEqual(product.Weight, product2.Weight);
                Assert.AreEqual(product.Category, product2.Category);

                var specification2 = product2.Specification as ComputerSpecification;
                Assert.IsTrue(specification2 != null);
                Assert.AreEqual(specification.Cores, specification2.Cores);
                Assert.AreEqual(specification.RamSize, specification2.RamSize);
                Assert.AreEqual(specification.Description, specification2.Description);
            }

            // serializer - amqp
            {
                var specification = new CarSpecification() { Engine = "V6", HorsePower = 239, Description = "SUV" };
                var product = new Product() { Name = "Car", Price = 34998, Weight = 5500, Specification = specification };
                var buffer = new ByteBuffer(1024, true);
                AmqpSerializer.Serialize(buffer, product);

                var value = Encoder.ReadObject(buffer) as DescribedValue;
                Assert.IsTrue(value != null);
                Assert.AreEqual(new Symbol("test.amqp:product"), value.Descriptor);

                var map = value.Value as Map;
                Assert.IsTrue(map != null);
                Assert.AreEqual(product.Name, map[new Symbol("Name")]);
                Assert.AreEqual(product.Price, map[new Symbol("Price")]);
                Assert.AreEqual(product.Weight, map[new Symbol("Weight")]);

                var specValue = map[new Symbol("Specification")] as DescribedValue;
                Assert.IsTrue(specValue != null);
                Assert.AreEqual(new Symbol("test.amqp:automotive-specification"), specValue.Descriptor);

                var specMap = specValue.Value as Map;
                Assert.IsTrue(specMap != null);
                Assert.AreEqual(specification.Engine, specMap[new Symbol("Engine")]);
                Assert.AreEqual(specification.HorsePower, specMap[new Symbol("HorsePower")]);
                Assert.AreEqual(specification.Description, specMap[new Symbol("Description")]);
            }

            // amqp - serializer
            {
                // keys MUST be symbols
                // the value types MUST match the field/property types in the class
                var specification = new DescribedValue(
                    new Symbol("test.amqp:automotive-specification"),
                    new Map()
                    {
                        { new Symbol("Engine"), "V8" },
                        { new Symbol("HorsePower"), 222 },
                        { new Symbol("Description"), "AWD SUV" },
                    });
                var product = new DescribedValue(
                    new Symbol("test.amqp:product"),
                    new Map()
                    {
                        { new Symbol("Name"), "Car" },
                        { new Symbol("Price"), 41200.0 },
                        { new Symbol("Weight"), 5600L },
                        { new Symbol("Specification"), specification },
                        { new Symbol("Category"), (sbyte)Category.Automotive }
                    });

                var buffer = new ByteBuffer(1024, true);
                Encoder.WriteObject(buffer, product);

                var product2 = AmqpSerializer.Deserialize<Product>(buffer);
                Assert.AreEqual("Car", product2.Name);
                Assert.AreEqual(41200.0, product2.Price);
                Assert.AreEqual(5600L, product2.Weight);
                Assert.AreEqual(Category.Automotive, product2.Category);

                var specification2 = product2.Specification as CarSpecification;
                Assert.IsTrue(specification2 != null);
                Assert.AreEqual("V8", specification2.Engine);
                Assert.AreEqual(222, specification2.HorsePower);
                Assert.AreEqual("AWD SUV", specification2.Description);
            }
        }

        [TestMethod()]
        public void AmqpSerializerSimpleMapEncodingTest()
        {
            // serializer test
            {
                var add = new AddOperation() { Version = 2, Name = "add", Param1 = 4, Param2 = 2 };
                var buffer = new ByteBuffer(1024, true);
                AmqpSerializer.Serialize(buffer, add);

                var add2 = AmqpSerializer.Deserialize<AddOperation>(buffer);
                Assert.AreEqual(add2.Name, add.Name);
                Assert.AreEqual(add2.Version, add.Version);
                Assert.AreEqual(add2.Param1, add.Param1);
                Assert.AreEqual(add2.Param2, add.Param2);
            }

            // serializer - amqp
            {
                var sqrt = new SquareRootOperation() { Version = 3, Name = "sqrt", Param = 64 };
                var buffer = new ByteBuffer(1024, true);
                AmqpSerializer.Serialize(buffer, sqrt);

                var map = Encoder.ReadObject(buffer) as Map;
                Assert.IsTrue(map != null);
                Assert.AreEqual(sqrt.Version, map["Version"]);
                Assert.AreEqual(sqrt.Name, map["Name"]);
                Assert.AreEqual(sqrt.Param, map["Param"]);
            }

            // amqp - serializer
            {
                var map = new Map()
                {
                    { "Version", 4 },
                    { "Name", "multi-op" },
                    { "Instruction", "Do add first and then SQRT" },
                    { "Add", new Map() { { "Param1", 100 }, { "Param2", 200} } },
                    { "SquareRoot", new Map() { { "Param", 81L } } },
                };

                var buffer = new ByteBuffer(1024, true);
                Encoder.WriteObject(buffer, map);

                var multi = AmqpSerializer.Deserialize<MultiOperation>(buffer);
                Assert.AreEqual(multi.Version, map["Version"]);
                Assert.AreEqual(multi.Name, map["Name"]);
                Assert.AreEqual(multi.Instruction, map["Instruction"]);

                var map1 = (Map)map["Add"];
                Assert.AreEqual(multi.Add.Param1, map1["Param1"]);
                Assert.AreEqual(multi.Add.Param2, map1["Param2"]);

                var map2 = (Map)map["SquareRoot"];
                Assert.AreEqual(multi.SquareRoot.Param, map2["Param"]);
            }
        }

        [TestMethod]
        public void AmqpSerializerMessageBodyTest()
        {
            MessageBodyTest<long>(
                1234567L,
                (x, y) => Assert.AreEqual(x, y));

            MessageBodyTest<string>(
                "tHis iS A sTrIng",
                (x, y) => Assert.AreEqual(x, y));

            MessageBodyTest<List<string>>(
                new List<string>() { "abc", "1k90" },
                (x, y) => CollectionAssert.AreEqual(x, y));

            MessageBodyTest<Dictionary<Symbol, string>>(
                new Dictionary<Symbol, string>() { { "product", "computer" }, { "company", "contoso" } },
                (x, y) => CollectionAssert.AreEqual(x, y));
        }

        [TestMethod]
        public void MessageSerializationTest()
        {
            var p1 = new Product() { Name = "test-product", Price = 34.99 };
            var inputMessage = new Message(p1);
            inputMessage.Properties = new Properties() { MessageId = "12345" };
            inputMessage.ApplicationProperties = new ApplicationProperties();
            inputMessage.ApplicationProperties["p1"] = "v1";
            inputMessage.ApplicationProperties["p2"] = 5ul;
            ByteBuffer buffer = inputMessage.Encode();

            // decode the message in a new app domain to ensure codec is intialized
            AppDomain ad = AppDomain.CreateDomain(
                "test-app-domain",
                AppDomain.CurrentDomain.Evidence,
                AppDomain.CurrentDomain.SetupInformation);
            ad.SetData("message-buffer", Convert.ToBase64String(buffer.Buffer, buffer.Offset, buffer.Length));
            ad.DoCallBack(() =>
            {
                byte[] b = Convert.FromBase64String(AppDomain.CurrentDomain.GetData("message-buffer") as string);
                try
                {
                    Message message = Message.Decode(new ByteBuffer(b, 0, b.Length, b.Length));
                    Product p2 = message.GetBody<Product>();
                    Assert.AreEqual("test-product", p2.Name);
                    Assert.AreEqual(34.99, p2.Price);
                    AppDomain.CurrentDomain.SetData("test-result", "pass");
                }
                catch (Exception exception)
                {
                    AppDomain.CurrentDomain.SetData("test-result", "fail:" + exception.Message);
                }

            });
            string result = ad.GetData("test-result") as string;
            AppDomain.Unload(ad);

            Assert.AreEqual("pass", result);
        }

        [TestMethod()]
        public void AmqpSerializerNegativeTest()
        {
            // List cannot have duplicate Order values
            {
                var value = new NegativeDuplicateOrder() { Field1 = 0, Field2 = 9, Field3 = 4 };
                NegativeTest(value, "Duplicate Order 1 detected in NegativeDuplicateOrder");
            }

            // Inherited class has different EncodingType than the base class
            {
                var value = new NegativeWrongBaseEncoding() { Name = "test", Field1 = 9 };
                NegativeTest(value, "NegativeWrongBaseEncoding.Encoding (List) is different from NegativeWrongBaseEncodingBase.Encoding (Map)");
            }

            // SimpleMap cannot support AmqpProvides
            {
                var value = new NegativeSimpleMapNoProvides() { Name = "test", Field1 = 9 };
                NegativeTest(value, "SimpleMap encoding does not include descriptors so it does not support AmqpProvidesAttribute");
            }
        }

        static void MessageBodyTest<T>(T value, Action<T, T> validator)
        {
            var inputMessage = new Message(value);
            var buffer = inputMessage.Encode();
            var outputMessage = Message.Decode(buffer);
            Assert.IsTrue(outputMessage.Body != null, "Body is not null");
            var value2 = outputMessage.GetBody<T>();
            validator(value, value2);
        }

        static void NegativeTest(object value, string error)
        {
            var buffer = new ByteBuffer(1024, true);
            try
            {
                AmqpSerializer.Serialize(buffer, value);
                Assert.IsTrue(false, "SerializationException not thrown");
            }
            catch (SerializationException e)
            {
                System.Diagnostics.Trace.WriteLine("Caught exception " + e.Message);
                Assert.IsTrue(e.Message.Contains(error));
            }
        }
    }
}
