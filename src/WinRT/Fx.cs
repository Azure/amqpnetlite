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

        public static void WaitOne(this WaitHandle waithandle, int msTimeout, bool unused)
        {
            waithandle.WaitOne(msTimeout);
        }

        public static uint ExtractValueFromArray(byte[] data, int pos, int size)
        {
            return size == 2 ? BitConverter.ToUInt16(data, pos) : BitConverter.ToUInt32(data, pos);
        }

        public static void InsertValueIntoArray(byte[] data, int pos, int size, uint val)
        {
            byte[] bytes = size == 2 ? BitConverter.GetBytes((ushort)val) : BitConverter.GetBytes(val);
            Buffer.BlockCopy(bytes, 0, data, pos, size);
        }

        public static float ReadFloat(ByteBuffer buffer)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(AmqpBitConverter.ReadInt(buffer)), 0);
        }

        public static double ReadDouble(ByteBuffer buffer)
        {
            return BitConverter.ToDouble(BitConverter.GetBytes(AmqpBitConverter.ReadLong(buffer)), 0);
        }

        public static void WriteFloat(ByteBuffer buffer, float data)
        {
            AmqpBitConverter.WriteInt(buffer, BitConverter.ToInt32(BitConverter.GetBytes(data), 0));
        }

        public static void WriteDouble(ByteBuffer buffer, double data)
        {
            AmqpBitConverter.WriteLong(buffer, BitConverter.ToInt64(BitConverter.GetBytes(data), 0));
        }

        public static string Format(string format, params object[] args)
        {
            return string.Format(format, args);
        }

#if WINDOWS_PHONE
        public static void StartThread(ThreadStart threadStart)
        {
            new Thread(threadStart).Start();
        }
#else
        public delegate void ThreadStart();

        public static void StartThread(ThreadStart threadStart)
        {
            System.Threading.Tasks.Task.Factory.StartNew(o => ((ThreadStart)o)(), threadStart);
        }
#endif
    }
}