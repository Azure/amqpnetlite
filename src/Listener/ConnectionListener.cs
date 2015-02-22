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

namespace Amqp.Listener
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Net.WebSockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Amqp.Framing;
    using Amqp.Sasl;
    using Amqp.Types;

    public class ConnectionListener : ConnectionFactory
    {
        readonly IContainer container;
        readonly TransportListener listener;
        readonly SaslMechanism[] saslMechanisms;
        readonly HashSet<Connection> connections;
        readonly Address address;

        public ConnectionListener(Uri addressUri, string userInfo, IContainer container)
        {
            this.connections = new HashSet<Connection>();
            this.saslMechanisms = CreateSaslMechanisms(userInfo);
            this.container = container;
            this.address = new Address(addressUri.Host, addressUri.Port, null, null, "/", addressUri.Scheme);
            if (addressUri.Scheme.Equals(Address.Amqp, StringComparison.OrdinalIgnoreCase))
            {
                this.listener = new TcpTransportListener(this, addressUri.Host, addressUri.Port);
            }
            else if (addressUri.Scheme.Equals(Address.Amqps, StringComparison.OrdinalIgnoreCase))
            {
                this.listener = new TlsTransportListener(this, addressUri.Host, addressUri.Port, container.ServiceCertificate);
            }
            else if (addressUri.Scheme.Equals(WebSocketTransport.WebSockets, StringComparison.OrdinalIgnoreCase))
            {
                this.listener = new WebSocketTransportListener(this, addressUri.Host, address.Port, address.Path, null);
            }
            else if (addressUri.Scheme.Equals(WebSocketTransport.SecureWebSockets, StringComparison.OrdinalIgnoreCase))
            {
                this.listener = new WebSocketTransportListener(this, addressUri.Host, address.Port, address.Path, container.ServiceCertificate);
            }
            else
            {
                throw new NotSupportedException(addressUri.Scheme);
            }
        }

        public IContainer Container
        {
            get { return this.container; }
        }

        public void Open()
        {
            this.listener.Open();
        }

        public void Close()
        {
            this.listener.Close();
        }

        static SaslMechanism[] CreateSaslMechanisms(string userInfo)
        {
            if (string.IsNullOrEmpty(userInfo))
            {
                return null;
            }

            string[] creds = userInfo.Split(':');
            string userName = Uri.UnescapeDataString(creds[0]);
            string password = creds.Length == 1 ? string.Empty : Uri.UnescapeDataString(creds[1]);
            return new SaslPlainMechanism[] { new SaslPlainMechanism(userName, password) };
        }

        async Task HandleTransportAsync(IAsyncTransport transport)
        {
            if (this.saslMechanisms != null)
            {
                ListenerSasProfile profile = new ListenerSasProfile(this);
                transport = await profile.OpenAsync(null, transport);
            }

            Connection connection = new ListenerConnection(this, this.address, transport);
            connection.Closed += this.OnConnectionClosed;
            lock (this.connections)
            {
                this.connections.Add(connection);
            }

            AsyncPump pump = new AsyncPump(transport);
            pump.Start(connection);
        }

        void OnConnectionClosed(AmqpObject sender, Error error)
        {
            lock (this.connections)
            {
                this.connections.Remove((Connection)sender);
            }
        }

        class ListenerSasProfile : SaslProfile
        {
            readonly ConnectionListener listener;
            SaslProfile innerProfile;

            public ListenerSasProfile(ConnectionListener listener)
            {
                this.listener = listener;
            }

            public SaslProfile InnerProfile
            {
                get { return this.innerProfile; }
            }

            protected override ITransport UpgradeTransport(ITransport transport)
            {
                return transport;
            }

            protected override DescribedList GetStartCommand(string hostname)
            {
                Symbol[] symbols = new Symbol[this.listener.saslMechanisms.Length];
                for (int i = 0; i < symbols.Length; i++)
                {
                    symbols[i] = this.listener.saslMechanisms[i].Name;
                }

                return new SaslMechanisms() { SaslServerMechanisms = symbols };
            }

            protected override DescribedList OnCommand(DescribedList command)
            {
                if (this.innerProfile == null)
                {
                    if (command.Descriptor.Code == Codec.SaslInit.Code)
                    {
                        var init = (SaslInit)command;
                        for (int i = 0; i < this.listener.saslMechanisms.Length; i++)
                        {
                            if (this.listener.saslMechanisms[i].Name == (string)init.Mechanism)
                            {
                                this.innerProfile = this.listener.saslMechanisms[i].CreateProfile();
                                break;
                            }
                        }

                        if (this.innerProfile == null)
                        {
                            throw new AmqpException(ErrorCode.NotImplemented, init.Mechanism);
                        }
                    }
                    else
                    {
                        throw new AmqpException(ErrorCode.NotAllowed, command.Descriptor.Name);
                    }
                }

                return this.innerProfile.OnCommandInternal(command);
            }
        }

        abstract class TransportListener
        {
            protected bool closed;

            protected ConnectionListener Listener
            {
                get;
                set;
            }

            public abstract void Open();

            public abstract void Close();
        }

        class TcpTransportListener : TransportListener
        {
            Socket[] listenSockets;

            public TcpTransportListener(ConnectionListener listener, string host, int port)
            {
                this.Listener = listener;

                List<IPAddress> addresses = new List<IPAddress>();
                IPAddress ipAddress;
                if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                    host.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                    host.Equals(Dns.GetHostEntry(string.Empty).HostName, StringComparison.OrdinalIgnoreCase))
                {
                    if (Socket.OSSupportsIPv4)
                    {
                        addresses.Add(IPAddress.Any);
                    }

                    if (Socket.OSSupportsIPv6)
                    {
                        addresses.Add(IPAddress.IPv6Any);
                    }
                }
                else if (IPAddress.TryParse(host, out ipAddress))
                {
                    addresses.Add(ipAddress);
                }
                else
                {
                    addresses.AddRange(Dns.GetHostAddresses(host));
                }

                this.listenSockets = new Socket[addresses.Count];
                for (int i = 0; i < addresses.Count; ++i)
                {
                    this.listenSockets[i] = new Socket(addresses[i].AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                    this.listenSockets[i].Bind(new IPEndPoint(addresses[i], port));
                    this.listenSockets[i].Listen(20);
                }
            }

            public override void Open()
            {
                for (int i = 0; i < this.listenSockets.Length; i++)
                {
                    var task = this.AcceptAsync(this.listenSockets[i]);
                }
            }

            public override void Close()
            {
                this.closed = true;
                if (this.listenSockets != null)
                {
                    for (int i = 0; i < this.listenSockets.Length; i++)
                    {
                        if (this.listenSockets[i] != null)
                        {
                            this.listenSockets[i].Close();
                        }
                    }
                }
            }

            protected async Task HandleSocketAsync(Socket socket)
            {
                try
                {
                    if (this.Listener.tcpSettings != null)
                    {
                        this.Listener.tcpSettings.Configure(socket);
                    }

                    IAsyncTransport transport = await this.CreateTransportAsync(socket);

                    await this.Listener.HandleTransportAsync(transport);
                }
                catch (Exception exception)
                {
                    Trace.WriteLine(TraceLevel.Error, exception.ToString());
                    socket.Close();
                }
            }

            protected virtual Task<IAsyncTransport> CreateTransportAsync(Socket socket)
            {
                return Task.FromResult<IAsyncTransport>(new TcpTransport(socket));
            }

            async Task AcceptAsync(Socket socket)
            {
                while (!this.closed)
                {
                    try
                    {
                        Socket acceptSocket = await Task.Factory.FromAsync(
                            (c, s) => ((Socket)s).BeginAccept(c, s),
                            (r) => ((Socket)r.AsyncState).EndAccept(r),
                            socket);

                        var task = this.HandleSocketAsync(acceptSocket);
                    }
                    catch (Exception exception)
                    {
                        Trace.WriteLine(TraceLevel.Warning, exception.ToString());
                    }
                }

                socket.Close();
            }
        }

        class TlsTransportListener : TcpTransportListener
        {
            readonly X509Certificate2 certificate;

            public TlsTransportListener(ConnectionListener listener, string host, int port, X509Certificate2 certificate)
                : base(listener, host, port)
            {
                this.certificate = certificate;
            }

            protected override async Task<IAsyncTransport> CreateTransportAsync(Socket socket)
            {
                var sslStream = new SslStream(new NetworkStream(socket));
                if (this.Listener.sslSettings == null)
                {
                    await sslStream.AuthenticateAsServerAsync(this.certificate);
                }
                else
                {
                    await sslStream.AuthenticateAsServerAsync(this.certificate, this.Listener.sslSettings.ClientCertificates.Count > 0,
                        this.Listener.sslSettings.Protocols, this.Listener.sslSettings.CheckCertificateRevocation);
                }

                return new TcpTransport(sslStream);
            }
        }

        class WebSocketTransportListener : TransportListener
        {
            readonly ConnectionListener listener;
            HttpListener httpListener;

            public WebSocketTransportListener(ConnectionListener listener, string host, int port, string path, X509Certificate2 certificate)
            {
                this.listener = listener;

                // if certificate is set, it must be bound to host:port by netsh http command
                string address = string.Format("{0}://{1}:{2}{3}", certificate == null ? "http" : "https", host, port, path);
                this.httpListener = new HttpListener();
                this.httpListener.Prefixes.Add(address);
            }

            public override void Open()
            {
                this.httpListener.Start();
                var task = this.AcceptListenerContextLoop();
            }

            public override void Close()
            {
                this.closed = true;
                this.httpListener.Stop();
                this.httpListener.Close();
            }

            async Task HandleListenerContextAsync(HttpListenerContext context)
            {
                WebSocket webSocket = null;
                try
                {
                    var wsContext = await context.AcceptWebSocketAsync(WebSocketTransport.WebSocketSubProtocol);
                    var wsTransport = new WebSocketTransport(wsContext.WebSocket);
                    await this.listener.HandleTransportAsync(wsTransport);
                }
                catch(Exception exception)
                {
                    Trace.WriteLine(TraceLevel.Error, exception.ToString());
                    if (webSocket != null)
                    {
                        webSocket.Abort();
                    }
                }
            }

            async Task AcceptListenerContextLoop()
            {
                while (!this.closed)
                {
                    try
                    {
                        HttpListenerContext context = await this.httpListener.GetContextAsync();

                        var task = this.HandleListenerContextAsync(context);
                    }
                    catch (Exception exception)
                    {
                        Trace.WriteLine(TraceLevel.Error, exception.ToString());
                    }
                }
            }
        }
    }
}
