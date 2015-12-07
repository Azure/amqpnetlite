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
    using System.Threading.Tasks;
    using Amqp.Framing;
    using Amqp.Sasl;
    using Amqp.Types;

    /// <summary>
    /// Provides extension methods.
    /// </summary>
    public static class TaskExtensions
    {
#if NETFX
        static async Task<DeliveryState> GetTransactionalStateAsync(SenderLink sender)
        {
            return await Amqp.Transactions.ResourceManager.GetTransactionalStateAsync(sender);
        }
#endif
#if NETFX || DOTNET
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
        /// <param name="timeout">The timeout in seconds.</param>
        /// <returns></returns>
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
        /// <param name="sender">The link.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public static async Task SendAsync(this SenderLink sender, Message message)
        {
            DeliveryState txnState = null;
#if NETFX
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
        /// <param name="receiver">The link.</param>
        /// <param name="timeout">The timeout in seconds.</param>
        /// <returns></returns>
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
            ProtocolHeader header = saslProfile.Start(hostname, transport);

            AsyncPump pump = new AsyncPump(bufferManager, transport);

            await pump.PumpAsync(
                h => { saslProfile.OnHeader(header, h); return true; },
                b => { SaslCode code; return saslProfile.OnFrame(transport, b, out code); });

            return (IAsyncTransport)saslProfile.UpgradeTransportInternal(transport);
        }
    }
}