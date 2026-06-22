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
    using System.Linq;
    using global::Amqp;
    using global::Amqp.Types;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EncoderTests
    {
        [TestMethod]
        public void EncoderNestedListTest()
        {
            var nested = new List<List<int>>
            {
                new List<int> { 0, 1 },
                new List<int> { 2, 3 },
                new List<int> { 4 },
            };

            ByteBuffer b = new ByteBuffer(512, true);
            Encoder.WriteList(b, nested, true);

            var formatCode = Encoder.ReadFormatCode(b);
            var list = Encoder.ReadList(b, formatCode);
            Assert.IsTrue(list != null);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(2, (list[0] as List).Count);
            Assert.AreEqual(2, (list[1] as List).Count);
            Assert.AreEqual(1, (list[2] as List).Count);
        }

        [TestMethod]
        public void EncoderNestedNestedListTest()
        {
            var nested = new List<object>
            {
                new List<object> { 0, 1, new List<string> { "test" } },
                new List<int> { 2, 3 },
                new List<int> { 4 },
            };

            ByteBuffer b = new ByteBuffer(512, true);
            Encoder.WriteList(b, nested, true);

            var formatCode = Encoder.ReadFormatCode(b);
            var list = Encoder.ReadList(b, formatCode);
            Assert.IsTrue(list != null);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(3, (list[0] as List).Count);
            Assert.AreEqual(2, (list[1] as List).Count);
            Assert.AreEqual(1, (list[2] as List).Count);

            var list0 = list[0] as List;
            var subList = (list0[2] as List).Cast<string>().ToList();
            Assert.AreEqual(1, subList.Count);
            Assert.AreEqual("test", subList[0]);
        }

        // Verify that every AMQP 1.0 performative is encoded with its full field count even
        // when only mandatory fields are set. Receivers that access fields by fixed numeric
        // index (e.g. Python _pyamqp) must see the complete list length.
        [TestMethod]
        public void PerformativesEncodeFullFieldCount()
        {
            // (performative, expected field count per AMQP 1.0 spec)
            var cases = new (global::Amqp.Types.DescribedList frame, int expectedCount)[]
            {
                (new global::Amqp.Framing.Open { ContainerId = "test" }, 10),
                (new global::Amqp.Framing.Begin { NextOutgoingId = 0, IncomingWindow = 100, OutgoingWindow = 100 }, 8),
                (new global::Amqp.Framing.Attach { LinkName = "test", Handle = 0, Role = true }, 14),
                (new global::Amqp.Framing.Flow(), 11),
                (new global::Amqp.Framing.Transfer { Handle = 0 }, 11),
                (new global::Amqp.Framing.Dispose { Role = false }, 6),
                (new global::Amqp.Framing.Detach { Handle = 0 }, 3),
                (new global::Amqp.Framing.End(), 1),
                (new global::Amqp.Framing.Close(), 1),
            };

            foreach (var (frame, expectedCount) in cases)
            {
                var buffer = new ByteBuffer(512, true);
                frame.Encode(buffer);

                // Skip described-type prefix: 0x00 + ulong format code + descriptor value
                AmqpBitConverter.ReadUByte(buffer); // 0x00 (described type)
                byte ulongFc = AmqpBitConverter.ReadUByte(buffer);
                if (ulongFc == 0x53) // smallulong
                    AmqpBitConverter.ReadUByte(buffer);
                else // 0x80 ulong
                    AmqpBitConverter.ReadULong(buffer);

                // Read list header and extract count
                byte listFc = AmqpBitConverter.ReadUByte(buffer);
                int count;
                if (listFc == 0x45) // list0 — empty
                {
                    count = 0;
                }
                else if (listFc == 0xc0) // list8 — size(1) + count(1)
                {
                    AmqpBitConverter.ReadUByte(buffer); // size
                    count = AmqpBitConverter.ReadUByte(buffer);
                }
                else // list32 (0xd0) — size(4) + count(4)
                {
                    AmqpBitConverter.ReadUInt(buffer); // size
                    count = (int)AmqpBitConverter.ReadUInt(buffer);
                }

                Assert.AreEqual(expectedCount, count,
                    $"{frame.GetType().Name} expected {expectedCount} fields but encoded {count}");
            }
        }
    }
}
