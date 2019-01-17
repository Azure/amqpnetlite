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
    using System.Threading;
    using System.Threading.Tasks;
    using Amqp.Framing;
    using Amqp.Sasl;
    using Amqp.Types;

    // Task based APIs

    public partial interface IAmqpObject
    {
        /// <summary>
        /// Closes an AMQP object asynchronously using a default timeout.
        /// </summary>
        /// <returns>A Task for the asynchronous close operation.</returns>
        Task CloseAsync();

        /// <summary>
        /// Closes an AMQP object asynchronously.
        /// </summary>
        /// <param name="timeout">The time to wait for the task to complete. Refer to AmqpObject.Close for details.</param>
        /// <returns>A Task for the asynchronous close operation.</returns>
        /// <param name="error">The AMQP <see cref="Error"/> to send to the peer,
        /// indicating why the object is being closed.</param>
        Task CloseAsync(TimeSpan timeout, Error error);
    }

    public partial interface ILink
    {
        /// <summary>
        /// Detaches the link endpoint without closing it.
        /// </summary>
        /// <param name="error">The error causing a detach.</param>
        /// <returns>A Task for the asynchronous detach operation.</returns>
        /// <remarks>
        /// An exception will be thrown if the peer responded with an error
        /// or the link was closed instead of being detached.
        /// </remarks>
        Task DetachAsync(Error error);
    }

    public partial interface ISenderLink
    {
        /// <summary>
        /// Sends a message asynchronously using a default timeout.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>A Task for the asynchronous send operation.</returns>
        Task SendAsync(Message message);

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="timeout">The time to wait for the task to complete.</param>
        /// <returns>A Task for the asynchronous send operation.</returns>
        Task SendAsync(Message message, TimeSpan timeout);
    }

    public partial interface IReceiverLink
    {
        /// <summary>
        /// Receives a message asynchronously.
        /// </summary>
        /// <returns>A Task for the asynchronous receive operation. The result is a Message object
        /// if available within a default timeout; otherwise a null value.</returns>
        Task<Message> ReceiveAsync();

        /// <summary>
        /// Receives a message asynchronously.
        /// </summary>
        /// <param name="timeout">The time to wait for a message.</param>
        /// <returns>A Task for the asynchronous receive operation. The result is a Message object
        /// if available within the specified timeout; otherwise a null value.</returns>
        /// <remarks>
        /// Use TimeSpan.MaxValue or Timeout.InfiniteTimeSpan to wait infinitely. If TimeSpan.Zero is supplied,
        /// the task completes immediately.
        /// </remarks>
        Task<Message> ReceiveAsync(TimeSpan timeout);
    }

    public partial class AmqpObject
    {
        /// <summary>
        /// Closes an AMQP object asynchronously using a default timeout.
        /// </summary>
        /// <returns>A Task for the asynchronous close operation.</returns>
        public Task CloseAsync()
        {
            return this.CloseInternalAsync(DefaultTimeout);
        }

        /// <summary>
        /// Closes an AMQP object asynchronously.
        /// </summary>
        /// <param name="timeout">The time to wait for the task to complete. Refer to AmqpObject.Close for details.</param>
        /// <returns>A Task for the asynchronous close operation.</returns>
        /// <param name="error">The AMQP <see cref="Error"/> to send to the peer,
        /// indicating why the object is being closed.</param>
        public Task CloseAsync(TimeSpan timeout, Error error = null)
        {
            return this.CloseInternalAsync((int)timeout.TotalMilliseconds, error);
        }

        internal async Task CloseInternalAsync(int timeout = 60000, Error error = null)
        {
            if (this.CloseCalled)
            {
                return;
            }

            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            try
            {
                this.AddClosedCallback((o, e) =>
                {
                    if (e != null)
                    {
                        tcs.TrySetException(new AmqpException(e));
                    }
                    else
                    {
                        tcs.TrySetResult(null);
                    }
                });

                this.CloseInternal(0, error);
            }
            catch (Exception exception)
            {
                tcs.TrySetException(exception);
            }

#if !NETFX40
            Task task = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
            if (task != tcs.Task)
            {
                tcs.TrySetException(new TimeoutException(Fx.Format(SRAmqp.AmqpTimeout,
                    "close", timeout, this.GetType().Name)));
            }
#endif
            await tcs.Task;
        }
    }

    public partial class Link
    {
        /// <summary>
        /// Detaches the link endpoint without closing it.
        /// </summary>
        /// <param name="error">The error causing a detach.</param>
        /// <returns>A Task for the asynchronous detach operation.</returns>
        /// <remarks>
        /// An exception will be thrown if the peer responded with an error
        /// or the link was closed instead of being detached.
        /// </remarks>
        public Task DetachAsync(Error error = null)
        {
            this.detach = true;
            return this.CloseInternalAsync(DefaultTimeout, error);
        }
    }

    public partial class SenderLink
    {
        /// <summary>
        /// Sends a message asynchronously using a default timeout.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>A Task for the asynchronous send operation.</returns>
        public Task SendAsync(Message message)
        {
            return this.SendAsync(message, TimeSpan.FromMilliseconds(DefaultTimeout));
        }

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="timeout">The time to wait for the task to complete.</param>
        /// <returns>A Task for the asynchronous send operation.</returns>
        public async Task SendAsync(Message message, TimeSpan timeout)
        {
            DeliveryState txnState = null;
#if NETFX || NETFX40
            txnState = await TaskExtensions.GetTransactionalStateAsync(this).ConfigureAwait(false);
#endif

            try
            {
                await new SendTask(this, message, txnState, timeout).Task.ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                this.OnTimeout(message);
                throw;
            }
        }
    }

    public partial class ReceiverLink
    {
        /// <summary>
        /// Receives a message asynchronously.
        /// </summary>
        /// <returns>A Task for the asynchronous receive operation. The result is a Message object
        /// if available within a default timeout; otherwise a null value.</returns>
        public Task<Message> ReceiveAsync()
        {
            return this.ReceiveAsync(TimeSpan.FromMilliseconds(DefaultTimeout));
        }

        /// <summary>
        /// Receives a message asynchronously.
        /// </summary>
        /// <param name="timeout">The time to wait for a message.</param>
        /// <returns>A Task for the asynchronous receive operation. The result is a Message object
        /// if available within the specified timeout; otherwise a null value.</returns>
        /// <remarks>
        /// Use TimeSpan.MaxValue or Timeout.InfiniteTimeSpan to wait infinitely. If TimeSpan.Zero is supplied,
        /// the task completes immediately.
        /// </remarks>
        public Task<Message> ReceiveAsync(TimeSpan timeout)
        {
            int waitTime = timeout == TimeSpan.MaxValue ? -1 : (int)(timeout.Ticks / 10000);
#if !NETFX40
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                waitTime = -1;
            }
#endif

            TaskCompletionSource<Message> tcs = new TaskCompletionSource<Message>();
            var message = this.ReceiveInternal(
                (l, m) =>
                {
                    if (l.Error != null)
                    {
                        tcs.TrySetException(new AmqpException(l.Error));
                    }
                    else
                    {
                        tcs.TrySetResult(m);
                    }
                },
                waitTime);

            if (message != null)
            {
                tcs.TrySetResult(message);
            }

            return tcs.Task;
        }
    }

    class SendTask : TaskCompletionSource<bool>
    {
        readonly static OutcomeCallback onOutcome = OnOutcome;
        readonly static TimerCallback onTimer = OnTimer;
        readonly Timer timer;

        public SendTask(SenderLink link, Message message, DeliveryState state, TimeSpan timeout)
        {
            this.timer = new Timer(onTimer, this, (int)timeout.TotalMilliseconds, -1);
            link.Send(message, state, onOutcome, this);
        }

        static void OnOutcome(ILink link, Message message, Outcome outcome, object state)
        {
            SendTask thisPtr = (SendTask)state;
            thisPtr.timer.Dispose();

            if (outcome.Descriptor.Code == Codec.Accepted.Code)
            {
                thisPtr.TrySetResult(true);
            }
            else if (outcome.Descriptor.Code == Codec.Rejected.Code)
            {
                thisPtr.TrySetException(new AmqpException(((Rejected)outcome).Error));
            }
            else if (outcome.Descriptor.Code == Codec.Released.Code)
            {
                thisPtr.TrySetException(new AmqpException(ErrorCode.MessageReleased, null));
            }
            else
            {
                thisPtr.TrySetException(new AmqpException(ErrorCode.InternalError, outcome.ToString()));
            }
        }

        static void OnTimer(object state)
        {
            var thisPtr = (SendTask)state;
            thisPtr.timer.Dispose();
            thisPtr.TrySetException(new TimeoutException());
        }
    }

    /// <summary>
    /// Provides extension methods for Task based APIs.
    /// </summary>
    public static class TaskExtensions
    {
#if NETFX || NETFX40 || NETSTANDARD2_0
        internal static async Task<DeliveryState> GetTransactionalStateAsync(SenderLink sender)
        {
            return await Amqp.Transactions.ResourceManager.GetTransactionalStateAsync(sender).ConfigureAwait(false);
        }
#endif

#if NETFX || DOTNET
        internal static async Task<System.Net.IPAddress[]> GetHostAddressesAsync(string host)
        {
            return await System.Net.Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
        }

        internal static async Task<System.Net.IPHostEntry> GetHostEntryAsync(string host)
        {
            return await System.Net.Dns.GetHostEntryAsync(host).ConfigureAwait(false);
        }
#endif

#if NETFX40
        internal static async Task<System.Net.IPAddress[]> GetHostAddressesAsync(string host)
        {
            return await Task.Factory.FromAsync(
                (c, s) => System.Net.Dns.BeginGetHostAddresses(host, c, s),
                (r) => System.Net.Dns.EndGetHostAddresses(r),
                null).ConfigureAwait(false);
        }

        internal static async Task<System.Net.IPHostEntry> GetHostEntryAsync(string host)
        {
            return await Task.Factory.FromAsync(
                (c, s) => System.Net.Dns.BeginGetHostEntry(host, c, s),
                (r) => System.Net.Dns.EndGetHostEntry(r),
                null).ConfigureAwait(false);
        }

        internal static async Task AuthenticateAsClientAsync(this System.Net.Security.SslStream source,
            string targetHost)
        {
            await Task.Factory.FromAsync(
                (c, s) => source.BeginAuthenticateAsClient(targetHost, c, s),
                (r) => source.EndAuthenticateAsClient(r),
                null).ConfigureAwait(false);
        }

        internal static async Task AuthenticateAsClientAsync(this System.Net.Security.SslStream source,
            string targetHost,
            System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates,
            System.Security.Authentication.SslProtocols enabledSslProtocols,
            bool checkCertificateRevocation)
        {
            await Task.Factory.FromAsync(
                (c, s) => source.BeginAuthenticateAsClient(targetHost, clientCertificates, enabledSslProtocols, checkCertificateRevocation, c, s),
                (r) => source.EndAuthenticateAsClient(r),
                null).ConfigureAwait(false);
        }

        internal static async Task AuthenticateAsServerAsync(this System.Net.Security.SslStream source,
            System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate)
        {
            await Task.Factory.FromAsync(
                (c, s) => source.BeginAuthenticateAsServer(serverCertificate, c, s),
                (r) => source.EndAuthenticateAsServer(r),
                null).ConfigureAwait(false);
        }

        internal static async Task AuthenticateAsServerAsync(this System.Net.Security.SslStream source,
            System.Security.Cryptography.X509Certificates.X509Certificate serverCertificate,
            bool clientCertificateRequired,
            System.Security.Authentication.SslProtocols enabledSslProtocols,
            bool checkCertificateRevocation)
        {
            await Task.Factory.FromAsync(
                (c, s) => source.BeginAuthenticateAsServer(serverCertificate, clientCertificateRequired, enabledSslProtocols, checkCertificateRevocation, c, s),
                (r) => source.EndAuthenticateAsServer(r),
                null).ConfigureAwait(false);
        }

        internal static async Task<int> ReadAsync(this System.Net.Security.SslStream source,
            byte[] buffer, int offset, int count)
        {
            return await Task.Factory.FromAsync(
                (c, s) => source.BeginRead(buffer, offset, count, c, s),
                (r) => source.EndRead(r),
                null).ConfigureAwait(false);
        }

        internal static async Task WriteAsync(this System.Net.Security.SslStream source,
            byte[] buffer, int offset, int count)
        {
            await Task.Factory.FromAsync(
                (c, s) => source.BeginWrite(buffer, offset, count, c, s),
                (r) => source.EndWrite(r),
                null).ConfigureAwait(false);
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

        internal static async Task<IAsyncTransport> OpenAsync(this SaslProfile saslProfile, string hostname,
            IBufferManager bufferManager, IAsyncTransport transport, DescribedList command)
        {
            // if transport is closed, pump reader should throw exception
            TransportWriter writer = new TransportWriter(transport, e => { });

            ProtocolHeader myHeader = saslProfile.Start(writer, command);

            AsyncPump pump = new AsyncPump(bufferManager, transport);
            SaslCode code = SaslCode.Auth;

            await pump.PumpAsync(
                SaslProfile.MaxFrameSize,
                header =>
                {
                    saslProfile.OnHeader(myHeader, header);
                    return true;
                },
                buffer =>
                {
                    return saslProfile.OnFrame(hostname, writer, buffer, out code);
                }).ConfigureAwait(false);

            await writer.FlushAsync().ConfigureAwait(false);

            if (code != SaslCode.Ok)
            {
                throw new AmqpException(ErrorCode.UnauthorizedAccess,
                    Fx.Format(SRAmqp.SaslNegoFailed, code));
            }

            return (IAsyncTransport)saslProfile.UpgradeTransportInternal(transport);
        }
    }
}