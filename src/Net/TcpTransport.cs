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
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    class TcpTransport : IAsyncTransport
    {
        static readonly RemoteCertificateValidationCallback noneCertValidator = (a, b, c, d) => true;
        readonly IBufferManager bufferManager;
        Connection connection;
        protected Writer writer;
        protected IAsyncTransport socketTransport;

        public TcpTransport()
            : this(null)
        {
        }

        public TcpTransport(IBufferManager bufferManager)
        {
            this.bufferManager = bufferManager;
        }

        public void Connect(Connection connection, Address address, bool noVerification)
        {
            this.connection = connection;
            var factory = new ConnectionFactory();
            if (noVerification)
            {
                factory.SSL.RemoteCertificateValidationCallback = noneCertValidator;
            }

            this.ConnectAsync(address, factory).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task ConnectAsync(Address address, ConnectionFactory factory)
        {
            IPAddress[] ipAddresses;
            IPAddress ip;
            if (IPAddress.TryParse(address.Host, out ip))
            {
                ipAddresses = new IPAddress[] { ip };
            }
            else
            {
                ipAddresses = await TaskExtensions.GetHostAddressesAsync(address.Host);
            }

            // need to handle both IPv4 and IPv6
            Socket socket = null;
            Exception exception = null;
            for (int i = 0; i < ipAddresses.Length; i++)
            {
                if (ipAddresses[i] == null ||
                    (ipAddresses[i].AddressFamily == AddressFamily.InterNetwork && !Socket.OSSupportsIPv4) ||
                    (ipAddresses[i].AddressFamily == AddressFamily.InterNetworkV6 && !Socket.OSSupportsIPv6))
                {
                    continue;
                }

                socket = new Socket(ipAddresses[i].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(ipAddresses[i], address.Port);
                    exception = null;
                    break;
                }
                catch (Exception e)
                {
                    exception = e;
                    socket.Dispose();
                    socket = null;
                }
            }

            if (socket == null)
            {
                throw exception ?? new SocketException((int)SocketError.AddressNotAvailable);
            }

            if (factory.tcpSettings != null)
            {
                factory.tcpSettings.Configure(socket);
            }

            IAsyncTransport transport;
            if (address.UseSsl)
            {
                SslStream sslStream;
                var ssl = factory.SslInternal;
                if (ssl == null)
                {
                    sslStream = new SslStream(new NetworkStream(socket));
                    await sslStream.AuthenticateAsClientAsync(address.Host);
                }
                else
                {
                    sslStream = new SslStream(new NetworkStream(socket), false, ssl.RemoteCertificateValidationCallback, ssl.LocalCertificateSelectionCallback);
                    await sslStream.AuthenticateAsClientAsync(address.Host, ssl.ClientCertificates,
                        ssl.Protocols, ssl.CheckCertificateRevocation);
                }

                transport = new SslSocket(this, sslStream);
            }
            else
            {
                transport = new TcpSocket(this, socket);
            }

            this.socketTransport = transport;
            this.writer = new Writer(this, this.socketTransport);
        }

        void IAsyncTransport.SetConnection(Connection connection)
        {
            this.connection = connection;
        }

        async Task<int> IAsyncTransport.ReceiveAsync(byte[] buffer, int offset, int count)
        {
            int received = await this.socketTransport.ReceiveAsync(buffer, offset, count);
            if (received == 0)
            {
                throw new SocketException((int)SocketError.ConnectionReset);
            }

            return received;
        }

        bool IAsyncTransport.SendAsync(ByteBuffer buffer, IList<ArraySegment<byte>> bufferList, int listSize)
        {
            throw new InvalidOperationException();
        }

        void ITransport.Close()
        {
            this.writer.Close();
        }

        void ITransport.Send(ByteBuffer buffer)
        {
            this.writer.Write(buffer);
        }

        int ITransport.Receive(byte[] buffer, int offset, int count)
        {
            int received = this.socketTransport.Receive(buffer, offset, count);
            if (received == 0)
            {
                throw new SocketException((int)SocketError.ConnectionReset);
            }

            return received;
        }

        void OnWriteSuccess()
        {
            this.writer.DisposeWriteBuffers();
            this.writer.ContinueWrite();
        }

        void OnWriteFailure(Exception exception)
        {
            this.connection.OnIoException(exception);
            this.writer.DisposeWriteBuffers();
            this.writer.DisposeQueuedBuffers();
        }

        protected class TcpSocket : IAsyncTransport
        {
            readonly static EventHandler<SocketAsyncEventArgs> onWriteComplete = OnWriteComplete;
            readonly TcpTransport transport;
            readonly Socket socket;
            readonly SocketAsyncEventArgs sendArgs;
            readonly SocketAsyncEventArgs receiveArgs;
            readonly IopsTracker receiveTracker;
            ByteBuffer receiveBuffer;

            public TcpSocket(TcpTransport transport, Socket socket)
            {
                this.transport = transport;
                this.socket = socket;
                this.receiveTracker = new IopsTracker();
                this.sendArgs = new SocketAsyncEventArgs();
                this.sendArgs.Completed += onWriteComplete;
                this.sendArgs.UserToken = this;

                this.receiveArgs = new SocketAsyncEventArgs();
                this.receiveArgs.Completed += (s, a) => ((TaskCompletionSource<int>)a.UserToken).Complete(a, b => b.BytesTransferred);
            }

            static void OnWriteComplete(object sender, SocketAsyncEventArgs eventArgs)
            {
                var thisPtr = (TcpSocket)eventArgs.UserToken;
                if (eventArgs.SocketError != SocketError.Success)
                {
                    thisPtr.transport.OnWriteFailure(new SocketException((int)eventArgs.SocketError));
                }
                else
                {
                    thisPtr.transport.OnWriteSuccess();
                }
            }

            void IAsyncTransport.SetConnection(Connection connection)
            {
                throw new NotImplementedException();
            }

            bool IAsyncTransport.SendAsync(ByteBuffer buffer, IList<ArraySegment<byte>> bufferList, int listSize)
            {
                if (buffer != null)
                {
                    this.sendArgs.BufferList = null;
                    this.sendArgs.SetBuffer(buffer.Buffer, buffer.Offset, buffer.Length);
                }
                else
                {
                    this.sendArgs.SetBuffer(null, 0, 0);
                    this.sendArgs.BufferList = bufferList;
                }

                return this.socket.SendAsync(this.sendArgs);
            }

            async Task<int> IAsyncTransport.ReceiveAsync(byte[] buffer, int offset, int count)
            {
                if (this.receiveBuffer != null && this.receiveBuffer.Length > 0)
                {
                    try
                    {
                        this.receiveBuffer.AddReference();
                        return ReceiveFromBuffer(this.receiveBuffer, buffer, offset, count);
                    }
                    finally
                    {
                        this.receiveBuffer.ReleaseReference();
                    }
                }

                if (this.receiveTracker.TrackOne())
                {
                    if (this.receiveBuffer != null)
                    {
                        this.receiveBuffer.ReleaseReference();
                    }

                    if (this.receiveTracker.Level > 0)
                    {
                        this.receiveBuffer = new ByteBuffer(8 * 1024, false);
                    }
                    else
                    {
                        this.receiveBuffer = null;
                    }
                }

                if (this.receiveBuffer == null)
                {
                    return await this.socket.ReceiveAsync(this.receiveArgs, buffer, offset, count);
                }
                else
                {
                    try
                    {
                        this.receiveBuffer.AddReference();
                        int bytes = await this.socket.ReceiveAsync(this.receiveArgs, this.receiveBuffer.Buffer, 0, this.receiveBuffer.Size);
                        this.receiveBuffer.Append(bytes);
                        return ReceiveFromBuffer(this.receiveBuffer, buffer, offset, count);
                    }
                    finally
                    {
                        this.receiveBuffer.ReleaseReference();
                    }
                }
            }

            void ITransport.Send(ByteBuffer buffer)
            {
                throw new InvalidOperationException();
            }

            int ITransport.Receive(byte[] buffer, int offset, int count)
            {
                return ((IAsyncTransport)this).ReceiveAsync(buffer, offset, count).GetAwaiter().GetResult();
            }

            void ITransport.Close()
            {
                this.socket.Dispose();
                this.sendArgs.Dispose();
                this.receiveArgs.Dispose();

                var temp = this.receiveBuffer;
                if (temp != null)
                {
                    temp.ReleaseReference();
                }
            }

            static int ReceiveFromBuffer(ByteBuffer byteBuffer, byte[] buffer, int offset, int count)
            {
                int len = byteBuffer.Length;
                if (len <= count)
                {
                    Buffer.BlockCopy(byteBuffer.Buffer, byteBuffer.Offset, buffer, offset, len);
                    byteBuffer.Reset();
                    return len;
                }
                else
                {
                    Buffer.BlockCopy(byteBuffer.Buffer, byteBuffer.Offset, buffer, offset, count);
                    byteBuffer.Complete(count);
                    return count;
                }
            }
        }

        protected class SslSocket : IAsyncTransport
        {
            readonly TcpTransport transport;
            readonly SslStream sslStream;

            public SslSocket(TcpTransport transport, SslStream sslStream)
            {
                this.transport = transport;
                this.sslStream = sslStream;
            }

            void IAsyncTransport.SetConnection(Connection connection)
            {
                throw new NotImplementedException();
            }

            bool IAsyncTransport.SendAsync(ByteBuffer buffer, IList<ArraySegment<byte>> bufferList, int listSize)
            {
                ByteBuffer writeBuffer;
                if (buffer != null)
                {
                    buffer.AddReference();
                    writeBuffer = buffer;
                }
                else
                {
                    writeBuffer = this.transport.bufferManager.GetByteBuffer(listSize);
                    for (int i = 0; i < bufferList.Count; i++)
                    {
                        ArraySegment<byte> segment = bufferList[i];
                        Buffer.BlockCopy(segment.Array, segment.Offset, writeBuffer.Buffer, writeBuffer.WritePos, segment.Count);
                        writeBuffer.Append(segment.Count);
                    }
                }

                Task task = this.sslStream.WriteAsync(writeBuffer.Buffer, writeBuffer.Offset, writeBuffer.Length);
                bool pending = !task.IsCompleted;
                if (pending)
                {
                    task.ContinueWith(
                        (t, s) =>
                        {
                            var tuple = (Tuple<SslSocket, ByteBuffer>)s;
                            tuple.Item2.ReleaseReference();

                            var thisPtr = tuple.Item1;
                            if (t.IsFaulted)
                            {
                                thisPtr.transport.OnWriteFailure(t.Exception.InnerException);
                            }
                            else
                            {
                                thisPtr.transport.OnWriteSuccess();
                            }
                        },
                        Tuple.Create(this, writeBuffer));
                }
                else
                {
                    writeBuffer.ReleaseReference();
                }

                return pending;
            }

            Task<int> IAsyncTransport.ReceiveAsync(byte[] buffer, int offset, int count)
            {
                return this.sslStream.ReadAsync(buffer, offset, count);
            }

            void ITransport.Send(ByteBuffer buffer)
            {
                throw new InvalidOperationException();
            }

            int ITransport.Receive(byte[] buffer, int offset, int count)
            {
                return this.sslStream.Read(buffer, offset, count);
            }

            void ITransport.Close()
            {
                this.sslStream.Dispose();
            }
        }

        protected class Writer
        {
            readonly TcpTransport owner;
            readonly IAsyncTransport transport;
            Queue<ByteBuffer> bufferQueue;
            List<ByteBuffer> buffersInProgress;
            bool writing;
            bool closed;

            public Writer(TcpTransport owner, IAsyncTransport transport)
            {
                this.owner = owner;
                this.transport = transport;
                this.bufferQueue = new Queue<ByteBuffer>();
                this.buffersInProgress = new List<ByteBuffer>();
            }

            object SyncRoot
            {
                get { return this.bufferQueue; }
            }

            public void Close()
            {
                lock (this.SyncRoot)
                {
                    this.closed = true;
                    if (this.writing)
                    {
                        return;
                    }
                }

                this.transport.Close();
            }

            public void Write(ByteBuffer buffer)
            {
                lock (this.SyncRoot)
                {
                    if (this.writing)
                    {
                        this.bufferQueue.Enqueue(buffer);
                        return;
                    }

                    this.buffersInProgress.Add(buffer);
                    this.writing = true;
                }

                bool pending = false;
                try
                {
                    pending = this.transport.SendAsync(buffer, null, 0);
                    if (!pending)
                    {
                        this.ContinueWrite();
                    }
                }
                finally
                {
                    if (!pending)
                    {
                        this.DisposeWriteBuffers();
                    }
                }
            }

            public void DisposeWriteBuffers()
            {
                lock (this.SyncRoot)
                {
                    for (int i = 0; i < this.buffersInProgress.Count; i++)
                    {
                        this.buffersInProgress[i].ReleaseReference();
                    }

                    this.buffersInProgress.Clear();
                }
            }

            public void DisposeQueuedBuffers()
            {
                lock (this.SyncRoot)
                {
                    foreach (var buffer in this.bufferQueue)
                    {
                        buffer.ReleaseReference();
                    }

                    this.bufferQueue.Clear();
                }
            }

            public void ContinueWrite()
            {
                ByteBuffer buffer = null;
                IList<ArraySegment<byte>> buffers = null;
                int listSize = 0;
                do
                {
                    lock (this.SyncRoot)
                    {
                        int queueDepth = this.bufferQueue.Count;
                        if (queueDepth == 0)
                        {
                            this.writing = false;
                            if (this.closed)
                            {
                                this.transport.Close();
                            }

                            return;
                        }
                        else if (queueDepth == 1)
                        {
                            buffer = this.bufferQueue.Dequeue();
                            this.buffersInProgress.Add(buffer);
                            buffers = null;
                        }
                        else
                        {
                            buffer = null;
                            listSize = 0;
                            buffers = new ArraySegment<byte>[queueDepth];
                            for (int i = 0; i < queueDepth; i++)
                            {
                                ByteBuffer item = this.bufferQueue.Dequeue();
                                this.buffersInProgress.Add(item);
                                buffers[i] = new ArraySegment<byte>(item.Buffer, item.Offset, item.Length);
                                listSize += item.Length;
                            }
                        }
                    }

                    bool pending = false;
                    try
                    {
                        pending = this.transport.SendAsync(buffer, buffers, listSize);
                        if (pending)
                        {
                            break;
                        }
                    }
                    catch (Exception exception)
                    {
                        this.owner.connection.OnIoException(exception);
                        this.DisposeQueuedBuffers();
                        break;
                    }
                    finally
                    {
                        if (!pending)
                        {
                            this.DisposeWriteBuffers();
                        }
                    }
                }
                while (true);
            }
        }

        class IopsTracker
        {
            const long WindowTicks = 6 * 1000 * 10000;
            static int[] thresholds = new int[] { 6000, 60000 };
            DateTime windowStart;
            int iops;
            byte level0;
            byte level1;

            public IopsTracker()
            {
                this.windowStart = DateTime.UtcNow;
            }

            public byte Level
            {
                get { return this.level0; }
            }

            public bool TrackOne()
            {
                this.iops++;
                var now = DateTime.UtcNow;
                bool changed = false;
                if (now.Ticks - this.windowStart.Ticks >= WindowTicks)
                {
                    int i = thresholds.Length;
                    while (i >= 1 && this.iops < thresholds[i - 1])
                    {
                        i--;
                    }

                    byte level = (byte)i;
                    if (this.level1 > this.level0)
                    {
                        if (level > this.level0)
                        {
                            this.level0 = Math.Min(level, this.level1);
                            changed = true;
                        }
                    }
                    else if (this.level1 < this.level0)
                    {
                        if (level < this.level0)
                        {
                            this.level0 = Math.Max(level0, this.level1);
                            changed = true;
                        }
                    }

                    this.level1 = level;
                    this.iops = 0;
                    this.windowStart = now;
                }

                return changed;
            }
        }
    }
}