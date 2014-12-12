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
using VSAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Test.Amqp
{
    static class Assert
    {
        public static void IsTrue(bool condition, string message = null)
        {
            VSAssert.IsTrue(condition, message ?? "Condition is not true.");
        }

        public static void AreEqual(object expected, object actual, string message = null)
        {
            VSAssert.AreEqual(expected, actual, message ?? "Objects are not equal.");
        }
    }
}
