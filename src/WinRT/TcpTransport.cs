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
    using System.Threading.Tasks;
    using Windows.Networking;
    using Windows.Networking.Sockets;
    using Windows.Storage.Streams;

    sealed class TcpTransport : IAsyncTransport
    {
        Connection connection;
        StreamSocket socket;

        public TcpTransport()
        {
        }

        public void Connect(Connection connection, Address address, bool noVerification)
        {
            this.connection = connection;
            var factory = new ConnectionFactory();
            this.ConnectAsync(address, factory).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void SetConnection(Connection connection)
        {
            this.connection = connection;
        }

        public async Task ConnectAsync(Address address, ConnectionFactory factory)
        {
            SocketProtectionLevel spl = !address.UseSsl ?
                SocketProtectionLevel.PlainSocket :
#if NETFX_CORE
                SocketProtectionLevel.Tls12;
#else
                SocketProtectionLevel.Ssl;
#endif
            StreamSocket streamSocket = new StreamSocket();
            streamSocket.Control.NoDelay = true;

#if UWP
            if (factory.sslSettings != null)
            {
                spl = factory.sslSettings.ProtectionLevel;
                streamSocket.Control.ClientCertificate = factory.sslSettings.ClientCertificate;
            }
#endif

            await streamSocket.ConnectAsync(new HostName(address.Host), address.Port.ToString(), spl);
            this.socket = streamSocket;
        }

        public async Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            var ret = await this.socket.InputStream.ReadAsync(buffer.AsBuffer(offset, count), (uint)count, InputStreamOptions.None);
            return (int)ret.Length;
        }

        public async Task SendAsync(IList<ByteBuffer> bufferList, int listSize)
        {
            for (int i = 0; i < bufferList.Count; i++)
            {
                ByteBuffer segment = bufferList[i];
                await this.socket.OutputStream.WriteAsync(segment.Buffer.AsBuffer(segment.Offset, segment.Length));
            }
        }

        public void Close()
        {
            this.socket.Dispose();
        }

        public void Send(ByteBuffer buffer)
        {
            this.SendAsync(new ByteBuffer[] { buffer }, buffer.Length).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public int Receive(byte[] buffer, int offset, int count)
        {
            return this.ReceiveAsync(buffer, offset, count).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}