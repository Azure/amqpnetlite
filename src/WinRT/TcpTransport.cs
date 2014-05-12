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
    using System.Collections.Generic;
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
    using Windows.Networking;
    using Windows.Networking.Sockets;
    using Windows.Storage.Streams;

    sealed class TcpTransport : ITransport
    {
        readonly Queue<ByteBuffer> writeQueue;
        StreamSocket socket;

        public TcpTransport()
        {
            this.writeQueue = new Queue<ByteBuffer>();
        }

        public bool ConnectAsync(string hostname, int port, string sslHost,
            bool noVerification, TransportCallback callback, object state)
        {
            this.socket = new StreamSocket();
            IAsyncAction action = this.socket.ConnectAsync(
                new HostName(hostname),
                port.ToString(),
                sslHost != null ? SocketProtectionLevel.Ssl : SocketProtectionLevel.PlainSocket);
            bool pending = action.Status == AsyncStatus.Started;
            if (pending)
            {
                this.AwaitConnect(action, callback, state);
            }

            return pending;
        }

        public void Close()
        {
            this.socket.Dispose();
        }

        public void Send(ByteBuffer buffer)
        {
            lock (this.writeQueue)
            {
                this.writeQueue.Enqueue(buffer);
                if (this.writeQueue.Count > 1)
                {
                    return;
                }
            }

            this.SendAsync(buffer);
        }

        public int Receive(byte[] buffer, int offset, int count)
        {
            IBuffer result = this.socket.InputStream.ReadAsync(buffer.AsBuffer(offset, count), (uint)count, InputStreamOptions.Partial).AsTask().Result;
            return (int)result.Length;
        }

        async void AwaitConnect(IAsyncAction action, TransportCallback callback, object state)
        {
            Exception exception = null;

            try
            {
                await action;
            }
            catch (Exception exp)
            {
                exception = exp;
            }

            callback(this, false, exception, state);
        }

        async void SendAsync(ByteBuffer buffer)
        {
            while (buffer != null)
            {
                await this.socket.OutputStream.WriteAsync(buffer.Buffer.AsBuffer(buffer.Offset, buffer.Length));

                lock (this.writeQueue)
                {
                    this.writeQueue.Dequeue();
                    if (this.writeQueue.Count == 0)
                    {
                        buffer = null;
                    }
                    else
                    {
                        buffer = this.writeQueue.Peek();
                    }
                }
            }
        }
    }
}