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

namespace PerfTest
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Amqp;
    using Amqp.Framing;
    using System.Reflection;

    static class Extensions
    {
        public static TraceLevel TotraceLevel(this string level)
        {
            return GetTraceMapping()[level];
        }

        public static SenderSettleMode ToSenderSettleMode(this string mode)
        {
            ushort tuple = GetSettleModeMapping()[mode];
            return (SenderSettleMode)(tuple >> 8);
        }

        public static ReceiverSettleMode ToReceiverSettleMode(this string mode)
        {
            ushort tuple = GetSettleModeMapping()[mode];
            return (ReceiverSettleMode)(tuple & 0xFF);
        }

        public static void PrintArguments(this Type type)
        {
            StringBuilder sb = new StringBuilder();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var prop in properties)
            {
                var attribute = (ArgumentAttribute)prop.GetCustomAttribute(typeof(ArgumentAttribute));
                if (attribute != null)
                {
                    int width = 0;
                    if (attribute.Name != null)
                    {
                        sb.Append('-');
                        sb.Append('-');
                        sb.Append(attribute.Name);
                        width += attribute.Name.Length + 2;
                    }

                    if (attribute.Shortcut != null)
                    {
                        sb.Append(' ');
                        sb.Append('(');
                        sb.Append('-');
                        sb.Append(attribute.Shortcut);
                        sb.Append(')');
                        width += attribute.Shortcut.Length + 4;
                    }

                    if (width < 20)
                    {
                        sb.Append(' ', 20 - width);
                        width = 20;
                    }
                    else
                    {
                        sb.Append(' ');
                        width++;
                    }

                    if (attribute.Description != null)
                    {
                        sb.Append(attribute.Description);
                    }

                    if (attribute.Default != null)
                    {
                        sb.Append('\r');
                        sb.Append('\n');
                        sb.Append(' ', width);
                        sb.Append("default: ");
                        sb.Append(attribute.Default);
                    }

                    sb.Append('\r');
                    sb.Append('\n');
                }
            }

            Console.WriteLine(sb.ToString());
        }

        static Dictionary<string, TraceLevel> GetTraceMapping()
        {
            return new Dictionary<string, TraceLevel>()
            {
                { "err", TraceLevel.Error },
                { "warn", TraceLevel.Warning },
                { "info", TraceLevel.Information },
                { "verbose", TraceLevel.Verbose },
                { "frm", TraceLevel.Frame }
            };
        }

        static Dictionary<string, ushort> GetSettleModeMapping()
        {
            return new Dictionary<string, ushort>()
            {
                { "amo", ((ushort)SenderSettleMode.Settled << 8) | (ushort)ReceiverSettleMode.First },
                { "alo", ((ushort)SenderSettleMode.Unsettled << 8) | (ushort)ReceiverSettleMode.First },
                { "eo", ((ushort)SenderSettleMode.Unsettled << 8) | (ushort)ReceiverSettleMode.Second }
            };
        }
    }
}
