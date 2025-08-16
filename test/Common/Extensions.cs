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
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using global::Amqp;
    using global::Amqp.Framing;
    using global::Amqp.Listener;

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

        public static void SetProperty(this Connection connection, string name, object value)
        {
            if (connection is ListenerConnection listenerConnection)
            {
                lock (listenerConnection)
                {
                    listenerConnection.Properties[name] = value;
                }
            }
        }

        public static T GetOrAddProperty<T>(this Connection connection, string name, Func<T> ctor = null)
        {
            if (connection is ListenerConnection listenerConnection)
            {
                lock (listenerConnection)
                {
                    if (listenerConnection.Properties.TryGetValue(name, out object obj))
                    {
                        return (T)obj;
                    }

                    if (ctor != null)
                    {
                        T value = ctor();
                        listenerConnection.Properties[name] = value;
                        return value;
                    }
                }
            }

            return default(T);
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

            return GetCertificate(certFindValue ?? host);
        }

        public static void ConfigureTest(this ConnectionListener listener)
        {
#if NET48
            // NET48 defaults to None protocol which does not work on Win10.
            // This can be removed if testing on Win11.
            listener.SSL.Protocols = System.Security.Authentication.SslProtocols.Tls12;
#endif
        }

        public static X509Certificate2 GetCertificate(string certFindValue)
        {
            X509Certificate2 cert = null;
            if (TryGetCertificate(StoreLocation.LocalMachine, StoreName.My, certFindValue, out cert))
            {
                return cert;
            }

            if (TryGetCertificate(StoreLocation.CurrentUser, StoreName.My, certFindValue, out cert))
            {
                return cert;
            }

            throw new ArgumentException("No certificate can be found using the find value " + certFindValue);
        }

        public static bool TryGetCertificate(StoreLocation storeLocation, StoreName storeName, string certFindValue, out X509Certificate2 cert)
        {
            X509Store store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.OpenExistingOnly);
            X509Certificate2Collection collection = store.Certificates.Find(
                X509FindType.FindBySubjectName,
                certFindValue,
                false);
            if (collection.Count == 0)
            {
                cert = null;
                return false;
            }

#if DOTNET
            store.Dispose();
#else
            store.Close();
#endif
            cert = collection[0];
            return true;
        }

#if NETFX40
        public static Task Yield()
        {
            return Task.Factory.StartNew(() => { });
        }

        public static Task<T> FromResult<T>(T result)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(result);
            return tcs.Task;
        }
#else

        public static YieldAwaitable Yield()
        {
            return Task.Yield();
        }

        public static Task<T> FromResult<T>(T result)
        {
            return Task.FromResult(result);
        }
#endif

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
