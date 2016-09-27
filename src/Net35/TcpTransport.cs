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
    using System.Net.Security;
    using System.Net.Sockets;

    sealed class TcpTransport : ITransport
    {
        static RemoteCertificateValidationCallback NoVerifyCallback = (a, b, c, d) => true;
        ITransport socketTransport;

        public void Connect(Connection connection, Address address, bool noVerification)
        {
            TcpSocket socket = new TcpSocket();
            socket.Connect(address.Host, address.Port);

            if (address.UseSsl)
            {
                SslSocket sslSocket = new SslSocket(socket, noVerification);
                sslSocket.AuthenticateAsClient(address.Host);
                this.socketTransport = sslSocket;
            }
            else
            {
                this.socketTransport = socket;
            }
        }

        public void Close()
        {
            if (this.socketTransport != null)
            {
                this.socketTransport.Close();
            }
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
            // support IPv4 only
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
            public SslSocket(Socket socket, bool noVerification)
                : base(new NetworkStream(socket), false, noVerification ? NoVerifyCallback : null)
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