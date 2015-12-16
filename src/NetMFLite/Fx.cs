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

namespace Amqp
{
    using System;
    using System.Diagnostics;
    using Microsoft.SPOT;

    static class Fx
    {
        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message)
        {
            Debug.Assert(condition, message);
        }

        public static void AssertAndThrow(ErrorCode id, bool condition)
        {
            if (!condition)
            {
                throw new Exception("Condition failed: " + id);
            }
        }

        [Conditional("TRACE")]
        public static void DebugPrint(bool send, ushort channel, string name, System.Collections.IList fields, params object[] fieldNames)
        {
#if TRACE
            System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
            sb.Append(send ? "SEND" : "RECV");
            sb.Append(' ');
            sb.Append(name);
            sb.Append('(');
            if (fields != null)
            {
                for (int i = 0; i < fields.Count && i < fieldNames.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(fieldNames[i]);
                    sb.Append(':');
                    object value = fields[i];
                    if (value != null)
                    {
                        if (value.GetType() == typeof(Amqp.Types.DescribedValue))
                        {
                            value = ((Amqp.Types.DescribedValue)value).Value;
                        }

                        var it = value as System.Collections.IEnumerable;
                        if (it != null)
                        {
                            sb.Append('[');
                            bool first = true;
                            foreach (var o in it)
                            {
                                if (!first)
                                {
                                    sb.Append(',');
                                }

                                sb.Append(o);
                                first = false;
                            }

                            sb.Append(']');
                        }
                        else
                        {
                            sb.Append(value);
                        }
                    }
                }
            }

            sb.Append(')');
            Microsoft.SPOT.Debug.Print(sb.ToString());
#endif
        }
    }
}