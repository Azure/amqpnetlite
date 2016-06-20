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
#if NETFX
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;

    class WebSocketTransport : IAsyncTransport
    {
        public const string WebSocketSubProtocol = "AMQPWSB10";
        public const string WebSockets = "WS";
        public const string SecureWebSockets = "WSS";
        const int WebSocketsPort = 80;
        const int SecureWebSocketsPort = 443;
        WebSocket webSocket;
        Connection connection;

        public WebSocketTransport()
        {
        }

        public WebSocketTransport(WebSocket webSocket)
        {
            this.webSocket = webSocket;
        }

        public static bool MatchScheme(string scheme)
        {
            return string.Equals(scheme, WebSockets, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(scheme, SecureWebSockets, StringComparison.OrdinalIgnoreCase);
        }

        public async Task ConnectAsync(Address address)
        {
            Uri uri = new UriBuilder()
            {
                Scheme = address.Scheme,
                Port = GetDefaultPort(address.Scheme, address.Port),
                Host = address.Host,
                Path = address.Path
            }.Uri;

            ClientWebSocket cws = new ClientWebSocket();
            cws.Options.AddSubProtocol(WebSocketSubProtocol);
            await cws.ConnectAsync(uri, CancellationToken.None);
            if (cws.SubProtocol != WebSocketSubProtocol)
            {
                cws.Abort();

                throw new NotSupportedException(
                    string.Format(
                    CultureInfo.InvariantCulture,
                    "WebSocket SubProtocol used by the host is not the same that was requested: {0}",
                    cws.SubProtocol ?? "<null>"));
            }

            this.webSocket = cws;
        }

        void IAsyncTransport.SetConnection(Connection connection)
        {
            this.connection = connection;
        }

        async Task<int> IAsyncTransport.ReceiveAsync(byte[] buffer, int offset, int count)
        {
            var result = await this.webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return 0;
            }

            return result.Count;
        }

        async Task IAsyncTransport.SendAsync(IList<ByteBuffer> bufferList, int listSize)
        {
            foreach (var buffer in bufferList)
            {
                await this.webSocket.SendAsync(new ArraySegment<byte>(buffer.Buffer, buffer.Offset, buffer.Length),
                    WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }

        void ITransport.Close()
        {
            this.webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", CancellationToken.None)
                .ContinueWith((t, o) => { if (t.IsFaulted) ((WebSocket)o).Dispose(); }, this.webSocket);
        }

        void ITransport.Send(ByteBuffer buffer)
        {
            this.webSocket.SendAsync(new ArraySegment<byte>(buffer.Buffer, buffer.Offset, buffer.Length),
                WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        int ITransport.Receive(byte[] buffer, int offset, int count)
        {
            return ((IAsyncTransport)this).ReceiveAsync(buffer, offset, count).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static int GetDefaultPort(string scheme, int port)
        {
            if (port < 0)
            {
                string temp = scheme.ToUpperInvariant();
                if (temp == WebSockets)
                {
                    port = WebSocketsPort;
                }
                else if (temp == SecureWebSockets)
                {
                    port = SecureWebSocketsPort;
                }
            }
            
            return port;
        }
    }
#endif
}