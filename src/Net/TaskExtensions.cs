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
    using System.Threading.Tasks;
    using Amqp.Framing;
    using Amqp.Sasl;
    using Amqp.Types;

    /// <summary>
    /// Provides extension methods.
    /// </summary>
    public static class TaskExtensions
    {
#if NETFX || NETFX40
        static async Task<DeliveryState> GetTransactionalStateAsync(SenderLink sender)
        {
            return await Amqp.Transactions.ResourceManager.GetTransactionalStateAsync(sender);
        }
#endif

#if NETFX || DOTNET
        internal static Task<System.Net.IPAddress[]> GetHostAddressesAsync(string host)
        {
            return System.Net.Dns.GetHostAddressesAsync(host);
        }

        internal static Task<System.Net.IPHostEntry> GetHostEntryAsync(string host)
        {
            return System.Net.Dns.GetHostEntryAsync(host);
        }
#endif

#if NETFX40
        internal static Task<System.Net.IPAddress[]> GetHostAddressesAsync(string host)
        {
            return Task.Factory.FromAsync(
                (c, s) => System.Net.Dns.BeginGetHostAddresses(host, c, s),
                (r) => System.Net.Dns.EndGetHostAddresses(r),
                null);
        }

        internal static Task<System.Net.IPHostEntry> GetHostEntryAsync(string host)
        {
            return Task.Factory.FromAsync(
                (c, s) => System.Net.Dns.BeginGetHostEntry(host, c, s),
                (r) => System.Net.Dns.EndGetHostEntry(r),
                null);
        }

        internal static Task AuthenticateAsClientAsync(this System.Net.Security.SslStream source,
            string targetHost)
        {
            return Task.Factory.FromAsync(
                (c, s) => source.BeginAuthenticateAsClient(targetHost, c, s),
                (r) => source.EndAuthenticateAsClient(r),
                null);
        }

        internal static Task AuthenticateAsClientAsync(this System.Net.Security.SslStream source,
            string targetHost,
            System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates,
            System.Security.Authentication.SslProtocols enabledSslProtocols,
            bool checkCertificateRevocation)
        {
            return Task.Factory.FromAsync(
                (c, s) => source.BeginAuthenticateAsClient(targetHost, clientCertificates, enabledSslProtocols, checkCertificateRevocation, c, s),
                (r) => source.EndAuthenticateAsClient(r),
                null);
        }

        internal static Task AuthenticateAsServerAsync(this System.Net.Security.SslStream source,
            System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate)
        {
            return Task.Factory.FromAsync(
                (c, s) => source.BeginAuthenticateAsServer(serverCertificate, c, s),
                (r) => source.EndAuthenticateAsServer(r),
                null);
        }

        internal static Task AuthenticateAsServerAsync(this System.Net.Security.SslStream source,
            System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate,
            bool clientCertificateRequired,
            System.Security.Authentication.SslProtocols enabledSslProtocols,
            bool checkCertificateRevocation)
        {
            return Task.Factory.FromAsync(
                (c, s) => source.BeginAuthenticateAsServer(serverCertificate, clientCertificateRequired, enabledSslProtocols, checkCertificateRevocation, c, s),
                (r) => source.EndAuthenticateAsServer(r),
                null);
        }

        internal static Task<int> ReadAsync(this System.Net.Security.SslStream source,
            byte[] buffer, int offset, int count)
        {
            return Task.Factory.FromAsync(
                (c, s) => source.BeginRead(buffer, offset, count, c, s),
                (r) => source.EndRead(r),
                null);
        }

        internal static Task WriteAsync(this System.Net.Security.SslStream source,
            byte[] buffer, int offset, int count)
        {
            return Task.Factory.FromAsync(
                (c, s) => source.BeginWrite(buffer, offset, count, c, s),
                (r) => source.EndWrite(r),
                null);
        }

        internal static Task ContinueWith(this Task task, Action<Task, object> action, object state)
        {
            return task.ContinueWith(t => action(t, state));
        }
#endif

#if NETFX || NETFX40 || DOTNET
        internal static ByteBuffer GetByteBuffer(this IBufferManager bufferManager, int size)
        {
            ByteBuffer buffer;
            if (bufferManager == null)
            {
                buffer = new ByteBuffer(size, true);
            }
            else
            {
                ArraySegment<byte> segment = bufferManager.TakeBuffer(size);
                buffer = new RefCountedByteBuffer(bufferManager, segment.Array, segment.Offset, segment.Count, 0);
            }

            return buffer;
        }
#else
        internal static ByteBuffer GetByteBuffer(this IBufferManager bufferManager, int size)
        {
            return new ByteBuffer(size, true);
        }
#endif

        /// <summary>
        /// Closes an AMQP object asynchronously.
        /// </summary>
        /// <param name="amqpObject">The object to close.</param>
        /// <param name="timeout">The timeout in milliseconds. Refer to AmqpObject.Close for details.</param>
        /// <returns>A Task for the asynchronous close operation.</returns>
        public static Task CloseAsync(this AmqpObject amqpObject, int timeout = 60000)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            try
            {
                amqpObject.Closed += (o, e) =>
                {
                    if (e != null)
                    {
                        tcs.SetException(new AmqpException(e));
                    }
                    else
                    {
                        tcs.SetResult(null);
                    }
                };

                amqpObject.Close(0);
            }
            catch (Exception exception)
            {
                tcs.SetException(exception);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <param name="sender">The sender link.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>A Task for the asynchronous send operation.</returns>
        public static async Task SendAsync(this SenderLink sender, Message message)
        {
            DeliveryState txnState = null;
#if NETFX || NETFX40
            txnState = await TaskExtensions.GetTransactionalStateAsync(sender);
#endif
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            sender.Send(
                message,
                txnState,
                (m, o, s) =>
                {
                    var t = (TaskCompletionSource<object>)s;
                    if (o.Descriptor.Code == Codec.Accepted.Code)
                    {
                        t.SetResult(null);
                    }
                    else if (o.Descriptor.Code == Codec.Rejected.Code)
                    {
                        t.SetException(new AmqpException(((Rejected)o).Error));
                    }
                    else
                    {
                        t.SetException(new AmqpException(ErrorCode.InternalError, o.Descriptor.Name));
                    }
                },
                tcs);

            await tcs.Task;
        }

        /// <summary>
        /// Receives a message asynchronously.
        /// </summary>
        /// <param name="receiver">The receiver link.</param>
        /// <param name="timeout">The timeout to wait for a message.</param>
        /// <returns>A Task for the asynchronous receive operation. The result is a Message object
        /// if available; otherwise a null value.</returns>
        public static Task<Message> ReceiveAsync(this ReceiverLink receiver, TimeSpan timeout)
        {
            return ReceiveAsync(receiver, (int)timeout.TotalMilliseconds);
        }

        /// <summary>
        /// Receives a message asynchronously.
        /// </summary>
        /// <param name="receiver">The receiver link.</param>
        /// <param name="timeout">The timeout in milliseconds to wait for a message.</param>
        /// <returns>A Task for the asynchronous receive operation. The result is a Message object
        /// if available; otherwise a null value.</returns>
        public static Task<Message> ReceiveAsync(this ReceiverLink receiver, int timeout = 60000)
        {
            TaskCompletionSource<Message> tcs = new TaskCompletionSource<Message>();
            try
            {
                var message = receiver.ReceiveInternal(
                    (l, m) => tcs.SetResult(m),
                    timeout);
                if (message != null)
                {
                    tcs.SetResult(message);
                }
            }
            catch (Exception exception)
            {
                tcs.SetException(exception);
            }

            return tcs.Task;
        }

        internal static async Task<IAsyncTransport> OpenAsync(this SaslProfile saslProfile, string hostname,
            IBufferManager bufferManager, IAsyncTransport transport)
        {
            // if transport is closed, pump reader should throw exception
            TransportWriter writer = new TransportWriter(transport, e => { });

            ProtocolHeader myHeader = saslProfile.Start(hostname, writer);

            AsyncPump pump = new AsyncPump(bufferManager, transport);

            await pump.PumpAsync(
                header =>
                {
                    saslProfile.OnHeader(myHeader, header);
                    return true;
                },
                buffer =>
                {
                    SaslCode code;
                    return saslProfile.OnFrame(writer, buffer, out code);
                });

            return (IAsyncTransport)saslProfile.UpgradeTransportInternal(transport);
        }
    }
}