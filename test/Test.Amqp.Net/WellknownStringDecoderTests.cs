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
using System.Text;
using Amqp;
using Amqp.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Encoder = Amqp.Types.Encoder;

namespace Test.Amqp
{
    [TestClass]
    public class WellknownStringDecoderTests
    {
        [TestMethod]
        public void UnknownString_CreatesNewInstance()
        {
            WellknownStringDecoder decoder = new WellknownStringDecoder();

            byte[] encodedBytes = Encoding.UTF8.GetBytes("abc");

            string decodedString1 = decoder.DecodeString(new ArraySegment<byte>(encodedBytes));
            string decodedString2 = decoder.DecodeString(new ArraySegment<byte>(encodedBytes));

            Assert.IsTrue(!ReferenceEquals(decodedString1, decodedString2));
        }

        [TestMethod]
        public void KnownString_SameInstanceReturned()
        {
            WellknownStringDecoder decoder = new WellknownStringDecoder();
            decoder.AddWellknownString("abc");

            byte[] encodedBytes = Encoding.UTF8.GetBytes("abc");

            string decodedString1 = decoder.DecodeString(new ArraySegment<byte>(encodedBytes));
            string decodedString2 = decoder.DecodeString(new ArraySegment<byte>(encodedBytes));

            Assert.IsTrue(ReferenceEquals(decodedString1, decodedString2));
        }

        [TestMethod]
        public void KnownString_SameStringCanBeAddedTwice()
        {
            WellknownStringDecoder decoder = new WellknownStringDecoder();
            decoder.AddWellknownString("abc");
            decoder.AddWellknownString("abc");
        }

        [TestMethod]
        public void KnownString_SameInstanceReturnedWithDifferentByteOffset()
        {
            WellknownStringDecoder decoder = new WellknownStringDecoder();
            decoder.AddWellknownString("abc");

            byte[] encodedBytes1 = Encoding.UTF8.GetBytes("abc");
            byte[] encodedBytes2 = Encoding.UTF8.GetBytes("12abc");

            string decodedString1 = decoder.DecodeString(new ArraySegment<byte>(encodedBytes1));
            string decodedString2 = decoder.DecodeString(new ArraySegment<byte>(encodedBytes2, 2, 3));

            Assert.IsTrue(ReferenceEquals(decodedString1, decodedString2));
        }

        [TestMethod]
        public void KnownString_UsedInEncoder()
        {
            try
            {
                WellknownStringDecoder decoder = new WellknownStringDecoder();
                decoder.AddWellknownString("abc");

                Encoder.StringDecoder = decoder;

                ByteBuffer buffer = new ByteBuffer(128, true);

                Encoder.WriteString(buffer, "abc", true);
                Encoder.WriteString(buffer, "abc", true);

                buffer.Seek(0);
                
                string decodedString1 = Encoder.ReadString(buffer, Encoder.ReadFormatCode(buffer));
                string decodedString2 = Encoder.ReadString(buffer, Encoder.ReadFormatCode(buffer));

                Assert.IsTrue(ReferenceEquals(decodedString1, decodedString2));
            }
            finally
            {
                Encoder.StringDecoder = null;
            }
        }
    }
}
