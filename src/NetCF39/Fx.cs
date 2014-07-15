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

namespace Amqp
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    // Framework specific routines
    public static class Fx
    {
        public static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;

        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message)
        {
            Debug.Assert(condition, message);
        }

        public static uint ExtractValueFromArray(byte[] data, int pos, int size)
        {
            throw new InvalidOperationException(".Net on Windows is always little endian");
        }

        public static void InsertValueIntoArray(byte[] data, int pos, int size, uint val)
        {
            throw new InvalidOperationException(".Net on Windows is always little endian");
        }

        public static unsafe float ReadFloat(ByteBuffer buffer)
        {
            float data;
            *(int*)&data = AmqpBitConverter.ReadInt(buffer);
            return data;
        }

        public static unsafe double ReadDouble(ByteBuffer buffer)
        {
            double data;
            *(long*)&data = AmqpBitConverter.ReadLong(buffer);
            return data;
        }

        public static unsafe void WriteFloat(ByteBuffer buffer, float data)
        {
            AmqpBitConverter.WriteInt(buffer, *(int*)&data);
        }

        public static unsafe void WriteDouble(ByteBuffer buffer, double data)
        {
            AmqpBitConverter.WriteLong(buffer, *(long*)&data);
        }

        public static string Format(string format, params object[] args)
        {
            return string.Format(format, args);
        }

        public static void StartThread(ThreadStart threadStart)
        {
            new Thread(threadStart).Start();
        }
    }
}