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
    }
}
