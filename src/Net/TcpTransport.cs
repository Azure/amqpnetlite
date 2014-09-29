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
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    sealed class TcpTransport : ITransport
    {
        Connection connection;
        Writer writer;
        IAsyncTransport socketTransport;

        public void Connect(Connection connection, Address address, bool noVerification)
        {
            IPAddress[] addressList;
            IPAddress ipHost;
            if (IPAddress.TryParse(address.Host, out ipHost))
            {
                addressList = new IPAddress[] { ipHost };
            }
            else
            {
                addressList = Dns.GetHostEntry(address.Host).AddressList;
            }

            Exception exception = null;
            TcpSocket socket = null;
            foreach (var ipAddress in addressList)
            {
                if (ipAddress == null)
                {
                    continue;
                }

                try
                {
                    socket = new TcpSocket(this, ipAddress.AddressFamily);
                    socket.Connect(new IPEndPoint(ipAddress, address.Port));
                    exception = null;
                    break;
                }
                catch (SocketException socketException)
                {
                    if (socket != null)
                    {
                        socket.Close();
                        socket = null;
                    }

                    exception = socketException;
                }
            }

            if (exception != null)
            {
                throw exception;
            }

            if (address.UseSsl)
            {
                SslSocket sslSocket = new SslSocket(this, socket, noVerification);
                sslSocket.AuthenticateAsClient(address.Host);
                this.socketTransport = sslSocket;
            }
            else
            {
                this.socketTransport = socket;
            }

            this.connection = connection;
            this.writer = new Writer(this, this.socketTransport);
        }

        public void Close()
        {
            this.socketTransport.Close();
        }

        public void Send(ByteBuffer buffer)
        {
            this.writer.Write(buffer);
        }

        public int Receive(byte[] buffer, int offset, int count)
        {
            return this.socketTransport.Receive(buffer, offset, count);
        }

        interface IAsyncTransport : ITransport
        {
            // true: pending, false: completed
            bool SendAsync(ByteBuffer buffer, IList<ArraySegment<byte>> bufferList, int listSize);
        }

        class TcpSocket : Socket, IAsyncTransport
        {
            readonly static EventHandler<SocketAsyncEventArgs> onWriteComplete = OnWriteComplete;
            readonly TcpTransport transport;
            readonly SocketAsyncEventArgs args;

            public TcpSocket(TcpTransport transport, AddressFamily addressFamily)
                : base(addressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                this.NoDelay = true;

                this.transport = transport;
                this.args = new SocketAsyncEventArgs();
                this.args.Completed += onWriteComplete;
                this.args.UserToken = this;
            }

            static void OnWriteComplete(object sender, SocketAsyncEventArgs eventArgs)
            {
                var thisPtr = (TcpSocket)eventArgs.UserToken;
                if (eventArgs.SocketError != SocketError.Success)
                {
                    thisPtr.transport.connection.OnIoException(new SocketException((int)eventArgs.SocketError));
                }
                else
                {
                    thisPtr.transport.writer.ContinueWrite();
                }
            }

            bool IAsyncTransport.SendAsync(ByteBuffer buffer, IList<ArraySegment<byte>> bufferList, int listSize)
            {
                if (buffer != null)
                {
                    this.args.BufferList = null;
                    this.args.SetBuffer(buffer.Buffer, buffer.Offset, buffer.Length);
                }
                else
                {
                    this.args.SetBuffer(null, 0, 0);
                    this.args.BufferList = bufferList;
                }

                return this.SendAsync(this.args);
            }

            void ITransport.Send(ByteBuffer buffer)
            {
                throw new InvalidOperationException();
            }

            int ITransport.Receive(byte[] buffer, int offset, int count)
            {
                return base.Receive(buffer, offset, count, SocketFlags.None);
            }

            void ITransport.Close()
            {
                base.Close();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.args.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        class SslSocket : SslStream, IAsyncTransport
        {
            static readonly RemoteCertificateValidationCallback noneCertValidator = (a, b, c, d) => true;
            readonly TcpTransport transport;

            public SslSocket(TcpTransport transport, Socket socket, bool noVarification)
                : base(new NetworkStream(socket), false, noVarification ? noneCertValidator : null)
            {
                this.transport = transport;
            }

            bool IAsyncTransport.SendAsync(ByteBuffer buffer, IList<ArraySegment<byte>> bufferList, int listSize)
            {
                ArraySegment<byte> writeBuffer;
                if (buffer != null)
                {
                    writeBuffer = new ArraySegment<byte>(buffer.Buffer, buffer.Offset, buffer.Length);
                }
                else
                {
                    byte[] temp = new byte[listSize];
                    int offset = 0;
                    for (int i = 0; i < bufferList.Count; i++)
                    {
                        ArraySegment<byte> segment = bufferList[i];
                        Buffer.BlockCopy(segment.Array, segment.Offset, temp, offset, segment.Count);
                        offset += segment.Count;
                    }

                    writeBuffer = new ArraySegment<byte>(temp, 0, listSize);
                }

                Task task = this.WriteAsync(writeBuffer.Array, writeBuffer.Offset, writeBuffer.Count);
                bool pending = !task.IsCompleted;
                if (pending)
                {
                    task.ContinueWith(
                        (t, s) =>
                        {
                            var thisPtr = (SslSocket)s;
                            if (t.IsFaulted)
                            {
                                thisPtr.transport.connection.OnIoException(t.Exception.InnerException);
                            }
                            else
                            {
                                thisPtr.transport.writer.ContinueWrite();
                            }
                        },
                        this);
                }

                return pending;
            }
            
            void ITransport.Send(ByteBuffer buffer)
            {
                throw new InvalidOperationException();
            }

            int ITransport.Receive(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, count);
            }

            void ITransport.Close()
            {
                base.Close();
            }
        }

        sealed class Writer
        {
            readonly TcpTransport owner;
            readonly IAsyncTransport transport;
            Queue<ByteBuffer> bufferQueue;
            bool writing;

            public Writer(TcpTransport owner, IAsyncTransport transport)
            {
                this.owner = owner;
                this.transport = transport;
                this.bufferQueue = new Queue<ByteBuffer>();
            }

            public void Write(ByteBuffer buffer)
            {
                lock (this.bufferQueue)
                {
                    if (this.writing)
                    {
                        this.bufferQueue.Enqueue(buffer);
                        return;
                    }

                    this.writing = true;
                }

                if (!this.transport.SendAsync(buffer, null, 0))
                {
                    this.ContinueWrite();
                }
            }

            public void ContinueWrite()
            {
                ByteBuffer buffer = null;
                IList<ArraySegment<byte>> buffers = null;
                int listSize = 0;
                do
                {
                    lock (this.bufferQueue)
                    {
                        int queueDepth = this.bufferQueue.Count;
                        if (queueDepth == 0)
                        {
                            this.writing = false;
                            return;
                        }
                        else if (queueDepth == 1)
                        {
                            buffer = this.bufferQueue.Dequeue();
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
                                buffers[i] = new ArraySegment<byte>(item.Buffer, item.Offset, item.Length);
                                listSize += item.Length;
                            }
                        }
                    }

                    try
                    {
                        if (this.transport.SendAsync(buffer, buffers, listSize))
                        {
                            break;
                        }
                    }
                    catch (Exception exception)
                    {
                        this.owner.connection.OnIoException(exception);
                        break;
                    }
                }
                while (true);
            }
        }
    }
}