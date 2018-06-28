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
        protected IAsyncTransport socketTransport;
        Connection connection;

        public TcpTransport()
            : this(null)
        {
        }

        public TcpTransport(IBufferManager bufferManager)
        {
            this.bufferManager = bufferManager;
        }

        public static bool MatchScheme(string scheme)
        {
            return string.Equals(scheme, Address.Amqp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(scheme, Address.Amqps, StringComparison.OrdinalIgnoreCase);
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
                ipAddresses = await TaskExtensions.GetHostAddressesAsync(address.Host).ConfigureAwait(false);
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

                    await socket.ConnectAsync(ipAddresses[i], address.Port).ConfigureAwait(false);

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
                    await sslStream.AuthenticateAsClientAsync(address.Host).ConfigureAwait(false);
                }
                else
                {
                    sslStream = new SslStream(new NetworkStream(socket), false, ssl.RemoteCertificateValidationCallback, ssl.LocalCertificateSelectionCallback);
                    await sslStream.AuthenticateAsClientAsync(address.Host, ssl.ClientCertificates,
                        ssl.Protocols, ssl.CheckCertificateRevocation).ConfigureAwait(false);
                }

                transport = new SslSocket(this, sslStream);
            }
            else
            {
                transport = new TcpSocket(this, socket);
            }

            this.socketTransport = transport;
        }

        void IAsyncTransport.SetConnection(Connection connection)
        {
            this.connection = connection;
        }

        Task<int> IAsyncTransport.ReceiveAsync(byte[] buffer, int offset, int count)
        {
            return this.socketTransport.ReceiveAsync(buffer, offset, count);
        }

        Task IAsyncTransport.SendAsync(IList<ByteBuffer> bufferList, int listSize)
        {
            return this.socketTransport.SendAsync(bufferList, listSize);
        }

        void ITransport.Close()
        {
            this.socketTransport.Close();
        }

        void ITransport.Send(ByteBuffer buffer)
        {
            this.socketTransport.Send(buffer);
        }

        int ITransport.Receive(byte[] buffer, int offset, int count)
        {
            return this.socketTransport.Receive(buffer, offset, count);
        }

        protected class TcpSocket : IAsyncTransport
        {
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
                this.sendArgs.Completed += (s, a) => SocketExtensions.Complete(s, a, true, 0);
                this.receiveArgs = new SocketAsyncEventArgs();
                this.receiveArgs.Completed += (s, a) => SocketExtensions.Complete(s, a, true, a.BytesTransferred);
            }

            void IAsyncTransport.SetConnection(Connection connection)
            {
                throw new NotImplementedException();
            }

            Task IAsyncTransport.SendAsync(IList<ByteBuffer> bufferList, int listSize)
            {
                var segments = new ArraySegment<byte>[bufferList.Count];
                for (int i = 0; i < bufferList.Count; i++)
                {
                    ByteBuffer f = bufferList[i];
                    segments[i] = new ArraySegment<byte>(f.Buffer, f.Offset, f.Length);
                }

                return this.socket.SendAsync(this.sendArgs, segments);
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
                    return await this.socket.ReceiveAsync(this.receiveArgs, buffer, offset, count).ConfigureAwait(false);
                }
                else
                {
                    try
                    {
                        this.receiveBuffer.AddReference();
                        int bytes = await this.socket.ReceiveAsync(this.receiveArgs, this.receiveBuffer.Buffer,
                            this.receiveBuffer.WritePos, this.receiveBuffer.Size).ConfigureAwait(false);
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
                this.socket.Send(buffer.Buffer, buffer.Offset, buffer.Length, SocketFlags.None);
            }

            int ITransport.Receive(byte[] buffer, int offset, int count)
            {
                return this.socket.Receive(buffer, offset, count, SocketFlags.None);
            }

            void ITransport.Close()
            {
                this.socket.Dispose();

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

            async Task IAsyncTransport.SendAsync(IList<ByteBuffer> bufferList, int listSize)
            {
                ByteBuffer writeBuffer;
                bool releaseBuffer = false;
                if (bufferList.Count == 1)
                {
                    writeBuffer = bufferList[0];
                }
                else
                {
                    releaseBuffer = true;
                    writeBuffer = this.transport.bufferManager.GetByteBuffer(listSize);
                    for (int i = 0; i < bufferList.Count; i++)
                    {
                        ByteBuffer segment = bufferList[i];
                        Buffer.BlockCopy(segment.Buffer, segment.Offset, writeBuffer.Buffer, writeBuffer.WritePos, segment.Length);
                        writeBuffer.Append(segment.Length);
                    }
                }

                try
                {
                    await this.sslStream.WriteAsync(writeBuffer.Buffer, writeBuffer.Offset, writeBuffer.Length).ConfigureAwait(false);
                }
                finally
                {
                    if (releaseBuffer)
                    {
                        writeBuffer.ReleaseReference();
                    }
                }
            }

            async Task<int> IAsyncTransport.ReceiveAsync(byte[] buffer, int offset, int count)
            {
                return await this.sslStream.ReadAsync(buffer, offset, count).ConfigureAwait(false);
            }

            void ITransport.Send(ByteBuffer buffer)
            {
                this.sslStream.Write(buffer.Buffer, buffer.Offset, buffer.Length);
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