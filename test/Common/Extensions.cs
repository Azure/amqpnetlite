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

namespace Test.Common
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using global::Amqp;
    using global::Amqp.Framing;

    static class Extensions
    {
        public static TraceLevel ToTraceLevel(this string level)
        {
            TraceLevel value;
            if (!GetTraceMapping().TryGetValue(level, out value))
            {
                throw new ArgumentException("Incorrect trace level " + level);
            }

            return value;
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

        public static X509Certificate2 GetCertificate(string scheme, string host, string certFindValue)
        {
            if (!scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (certFindValue == null)
            {
                certFindValue = host;
            }

            StoreLocation[] locations = new StoreLocation[] { StoreLocation.LocalMachine, StoreLocation.CurrentUser };
            foreach (StoreLocation location in locations)
            {
                X509Store store = new X509Store(StoreName.My, location);
                store.Open(OpenFlags.OpenExistingOnly);

                X509Certificate2Collection collection = store.Certificates.Find(
                    X509FindType.FindBySubjectName,
                    certFindValue,
                    false);

                if (collection.Count == 0)
                {
                    collection = store.Certificates.Find(
                        X509FindType.FindByThumbprint,
                        certFindValue,
                        false);
                }

                store.Close();

                if (collection.Count > 0)
                {
                    return collection[0];
                }
            }

            throw new ArgumentException("No certificate can be found using the find value " + certFindValue);
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
