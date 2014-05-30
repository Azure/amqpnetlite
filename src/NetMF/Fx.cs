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
    using Microsoft.SPOT;
    using Microsoft.SPOT.Hardware;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using System;

    // Framework specific routines
    public static class Fx
    {
        public static readonly bool IsLittleEndian = Fx.ExtractValueFromArray(new byte[] { 0x01, 0x02 }, 0, 2) == 0x0201u;

        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message)
        {
            Debug.Assert(condition, message);
        }

        public static uint ExtractValueFromArray(byte[] data, int pos, int size)
        {
            return Utility.ExtractValueFromArray(data, pos, size);
        }

        public static void InsertValueIntoArray(byte[] data, int pos, int size, uint val)
        {
            Utility.InsertValueIntoArray(data, pos, size, val);
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
#if MF_FRAMEWORK_VERSION_V4_2
            var tempBuffer = Microsoft.SPOT.Reflection.Serialize(data, typeof(double));
            AmqpBitConverter.WriteBytes(buffer, tempBuffer, 1, tempBuffer.Length-1);
#else
            AmqpBitConverter.WriteLong(buffer, *((long*)&data));
#endif
        }

        public static string Format(string format, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return format;
            }

            StringBuilder sb = new StringBuilder(format.Length * 2);

            char[] array = format.ToCharArray();
            for (int i = 0; i < array.Length; ++i)
            {
                // max supported number of args is 10
                if (array[i] == '{' && i + 2 < array.Length && array[i + 2] == '}' && array[i + 1] >= '0' && array[i + 1] <= '9')
                {
                    int index = array[i + 1] - '0';
                    if (index < args.Length)
                    {
                        sb.Append(args[index]);
                    }

                    i += 2;
                }
                else
                {
                    sb.Append(array[i]);
                }
            }

            return sb.ToString();
        }

        public static void StartThread(ThreadStart threadStart)
        {
            new Thread(threadStart).Start();
        }
    }

    class Uri
    {
        public static string UnescapeDataString(string url)
        {
            var data = Encoding.UTF8.GetBytes(url);
            return new string(Encoding.UTF8.GetChars(UrlDecodeBytesToBytes(data, 0, data.Length)));
        }

        static byte[] UrlDecodeBytesToBytes(byte[] bytes, int offset, int count)
        {
            var length = 0;
            var sourceArray = new byte[count];
            for (var i = 0; i < count; i++)
            {
                var index = offset + i;
                var num4 = bytes[index];
                if (num4 == 0x2b)
                {
                    num4 = 0x20;
                }
                else if ((num4 == 0x25) &&
                         (i < (count - 2)))
                {
                    var num5 = HexToInt((char)bytes[index + 1]);
                    var num6 = HexToInt((char)bytes[index + 2]);
                    if ((num5 >= 0) &&
                        (num6 >= 0))
                    {
                        num4 = (byte)((num5 << 4) | num6);
                        i += 2;
                    }
                }
                sourceArray[length++] = num4;
            }
            if (length < sourceArray.Length)
            {
                var destinationArray = new byte[length];
                Array.Copy(sourceArray, destinationArray, length);
                sourceArray = destinationArray;
            }
            return sourceArray;
        }

        static int HexToInt(char h)
        {
            if ((h >= '0') &&
                (h <= '9'))
            {
                return (h - '0');
            }
            if ((h >= 'a') &&
                (h <= 'f'))
            {
                return ((h - 'a') + 10);
            }
            if ((h >= 'A') &&
                (h <= 'F'))
            {
                return ((h - 'A') + 10);
            }
            return -1;
        }
    }
}