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
#if (MF_FRAMEWORK_VERSION_V4_2 || MF_FRAMEWORK_VERSION_V4_3 || MF_FRAMEWORK_VERSION_V4_4)
    using Microsoft.SPOT.Net.Security;
#elif (NANOFRAMEWORK_1_0)
    using System.Net.Security;
#endif
    using System;
    using System.Net;
    using System.Net.Sockets;

    sealed class TcpTransport : ITransport
    {
        ITransport socketTransport;

        public void Connect(Connection connection, Address address, bool noVerification)
        {
            var ipHostEntry = Dns.GetHostEntry(address.Host);
            Exception exception = null;
            TcpSocket socket = null;

            foreach (var ipAddress in ipHostEntry.AddressList)
            {
                if (ipAddress == null)
                {
                    continue;
                }

                try
                {
                    socket = new TcpSocket();
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
                SslSocket sslSocket = new SslSocket(socket);

#if (MF_FRAMEWORK_VERSION_V4_2 || MF_FRAMEWORK_VERSION_V4_3 || MF_FRAMEWORK_VERSION_V4_4)
                sslSocket.AuthenticateAsClient(
                    address.Host,
                    null,
                    noVerification ? SslVerification.NoVerification : SslVerification.VerifyPeer,
                    SslProtocols.Default);
#elif (NANOFRAMEWORK_1_0)

                sslSocket.SslVerification = noVerification ? SslVerification.NoVerification : SslVerification.VerifyPeer;

                sslSocket.AuthenticateAsClient(
                    address.Host,
                    null,
                    SslProtocols.Tls11);
#endif

                this.socketTransport = sslSocket;
            }
            else
            {
                this.socketTransport = socket;
            }
        }

        public void Close()
        {
            this.socketTransport.Close();
        }

        public void Send(ByteBuffer buffer)
        {
            this.socketTransport.Send(buffer);
        }

        public int Receive(byte[] buffer, int offset, int count)
        {
            return this.socketTransport.Receive(buffer, offset, count);
        }

        class TcpSocket : Socket, ITransport
        {
            public TcpSocket()
                : base(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
            }

            void ITransport.Send(ByteBuffer buffer)
            {
                base.Send(buffer.Buffer, buffer.Offset, buffer.Length, SocketFlags.None);
            }

            int ITransport.Receive(byte[] buffer, int offset, int count)
            {
                return base.Receive(buffer, offset, count, SocketFlags.None);
            }

            void ITransport.Close()
            {
                base.Close();
            }
        }

        class SslSocket : SslStream, ITransport
        {
            public SslSocket(Socket socket)
                : base(socket)
            {
            }

            void ITransport.Send(ByteBuffer buffer)
            {
                base.Write(buffer.Buffer, buffer.Offset, buffer.Length);
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
    }
}