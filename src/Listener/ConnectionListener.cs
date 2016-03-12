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
#if NETFX
    using System.Net.WebSockets;
#endif
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Amqp.Framing;
    using Amqp.Sasl;
    using Amqp.Types;

    /// <summary>
    /// The conneciton listener accepts AMQP connections from an address.
    /// </summary>
    public class ConnectionListener : ConnectionFactoryBase
    {
        readonly IContainer container;
        readonly HashSet<Connection> connections;
        readonly Address address;
        TransportListener listener;
        SslSettings sslSettings;
        SaslSettings saslSettings;
        bool closed;

        /// <summary>
        /// Initializes the connection listener object.
        /// </summary>
        /// <param name="addressUri"></param>
        /// <param name="userInfo"></param>
        /// <param name="container"></param>
        public ConnectionListener(Uri addressUri, string userInfo, IContainer container)
            : base()
        {
            this.connections = new HashSet<Connection>();
            this.container = container;

            string userName = null;
            string password = null;
            if (userInfo != null)
            {
                string[] creds = userInfo.Split(':');
                if (creds.Length != 2)
                {
                    throw new ArgumentException("userInfo");
                }

                userName = Uri.UnescapeDataString(creds[0]);
                password = creds.Length == 1 ? string.Empty : Uri.UnescapeDataString(creds[1]);
            }

            this.address = new Address(addressUri.Host, addressUri.Port, userName, password, addressUri.AbsolutePath, addressUri.Scheme);
        }

        /// <summary>
        /// Gets the AMQP container.
        /// </summary>
        public IContainer Container
        {
            get { return this.container; }
        }

        /// <summary>
        /// Gets the address the listener is listening on.
        /// </summary>
        public Address Address
        {
            get { return this.address; }
        }

        /// <summary>
        /// Gets the TLS/SSL settings on the listener.
        /// </summary>
        public SslSettings SSL
        {
            get
            {
                return this.sslSettings ?? (this.sslSettings = new SslSettings());
            }
        }

        /// <summary>
        /// Gets the SASL settings on the listener.
        /// </summary>
        public SaslSettings SASL
        {
            get
            {
                return this.saslSettings ?? (this.saslSettings = new SaslSettings());
            }
        }

        /// <summary>
        /// Opens the listener.
        /// </summary>
        public void Open()
        {
            if (this.closed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (this.address.Scheme.Equals(Address.Amqp, StringComparison.OrdinalIgnoreCase))
            {
                this.listener = new TcpTransportListener(this, this.address.Host, this.address.Port);
            }
            else if (this.address.Scheme.Equals(Address.Amqps, StringComparison.OrdinalIgnoreCase))
            {
                this.listener = new TlsTransportListener(this, this.address.Host, this.address.Port, this.GetServiceCertificate());
            }
#if NETFX
            else if (this.address.Scheme.Equals(WebSocketTransport.WebSockets, StringComparison.OrdinalIgnoreCase))
            {
                this.listener = new WebSocketTransportListener(this, this.address.Host, address.Port, address.Path, null);
            }
            else if (this.address.Scheme.Equals(WebSocketTransport.SecureWebSockets, StringComparison.OrdinalIgnoreCase))
            {
                this.listener = new WebSocketTransportListener(this, this.address.Host, address.Port, address.Path, this.GetServiceCertificate());
            }
#endif
            else
            {
                throw new NotSupportedException(this.address.Scheme);
            }

            if (this.address.User != null)
            {
                this.SASL.EnablePlainMechanism(this.address.User, this.address.Password);
            }

            this.listener.Open();
        }

        /// <summary>
        /// Closes the listener.
        /// </summary>
        public void Close()
        {
            this.listener.Close();

            var snapshot = new List<Connection>();
            lock (this.connections)
            {
                this.closed = true;
                snapshot.AddRange(this.connections);
                connections.Clear();
            }

            foreach (var connection in snapshot)
            {
                connection.Close(AmqpObject.DefaultCloseTimeout, new Error() { Condition = ErrorCode.ConnectionForced });
            }
        }

        X509Certificate2 GetServiceCertificate()
        {
            if (this.sslSettings != null && this.sslSettings.Certificate != null)
            {
                return this.sslSettings.Certificate;
            }
            else if (this.container.ServiceCertificate != null)
            {
                return this.container.ServiceCertificate;
            }

            throw new ArgumentNullException("certificate");
        }

        async Task HandleTransportAsync(IAsyncTransport transport)
        {
            IPrincipal principal = null;
            if (this.saslSettings != null)
            {
                ListenerSaslProfile profile = new ListenerSaslProfile(this);
                transport = await profile.OpenAsync(null, this.BufferManager, transport);
                principal = profile.GetPrincipal();
            }

            var connection = new ListenerConnection(this, this.address, transport);
            if (principal == null)
            {
                // SASL principal preferred. If not present, check transport.
                IAuthenticated authenticated = transport as IAuthenticated;
                if (authenticated != null)
                {
                    principal = authenticated.Principal;
                }
            }

            connection.Principal = principal;

            bool shouldClose = false;
            lock (this.connections)
            {
                if (!this.closed)
                {
                    connection.Closed += this.OnConnectionClosed;
                    this.connections.Add(connection);
                }
                else
                {
                    shouldClose = true;
                }
            }

            if (shouldClose)
            {
                await connection.CloseAsync();
            }
            else
            {
                AsyncPump pump = new AsyncPump(this.BufferManager, transport);
                pump.Start(connection);
            }
        }

        void OnConnectionClosed(AmqpObject sender, Error error)
        {
            lock (this.connections)
            {
                this.connections.Remove((Connection)sender);
            }
        }

        /// <summary>
        /// Contains the TLS/SSL settings for a connection.
        /// </summary>
        public class SslSettings
        {
            internal SslSettings()
            {
                this.Protocols = SslProtocols.Ssl3 | SslProtocols.Tls;
            }

            /// <summary>
            /// Gets or sets the listener certificate.
            /// </summary>
            public X509Certificate2 Certificate
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets a a Boolean value that specifies whether the client must supply a certificate for authentication.
            /// </summary>
            public bool ClientCertificateRequired
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the supported protocols to use.
            /// </summary>
            public SslProtocols Protocols
            {
                get;
                set;
            }

            /// <summary>
            /// Specifies whether certificate revocation should be performed during handshake.
            /// </summary>
            public bool CheckCertificateRevocation
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets a certificate validation callback to validate remote certificate.
            /// </summary>
            public RemoteCertificateValidationCallback RemoteCertificateValidationCallback
            {
                get;
                set;
            }
        }

        /// <summary>
        /// Contains the SASL settings for a connection.
        /// </summary>
        public class SaslSettings
        {
            Dictionary<Symbol, SaslMechanism> mechanisms;

            internal SaslSettings()
            {
                this.mechanisms = new Dictionary<Symbol, SaslMechanism>();
            }

            internal Symbol[] Mechanisms
            {
                get
                {
                    return new List<Symbol>(this.mechanisms.Keys).ToArray();
                }
            }

            /// <summary>
            /// Gets or sets a value indicating if SASL ANONYMOUS mechanism is enabled.
            /// </summary>
            public bool EnableAnonymousMechanism
            {
                get
                {
                    return this.mechanisms.ContainsKey(SaslProfile.AnonymousName);
                }

                set
                {
                    if (value)
                    {
                        this.mechanisms[SaslProfile.AnonymousName] = SaslMechanism.Anonymous;
                    }
                    else
                    {
                        this.mechanisms.Remove(SaslProfile.AnonymousName);
                    }
                }
            }

            /// <summary>
            /// Gets or sets a value indicating if SASL EXTERNAL mechanism is enabled.
            /// </summary>
            public bool EnableExternalMechanism
            {
                get
                {
                    return this.mechanisms.ContainsKey(SaslProfile.ExternalName);
                }

                set
                {
                    if (value)
                    {
                        this.mechanisms[SaslProfile.ExternalName] = SaslMechanism.External;
                    }
                    else
                    {
                        this.mechanisms.Remove(SaslProfile.ExternalName);
                    }
                }
            }

            /// <summary>
            /// Enables SASL PLAIN mechanism.
            /// </summary>
            /// <param name="userName"></param>
            /// <param name="password"></param>
            public void EnablePlainMechanism(string userName, string password)
            {
                this.mechanisms[SaslPlainProfile.Name] = new SaslPlainMechanism(userName, password);
            }

            internal bool TryGetMechanism(Symbol name, out SaslMechanism mechanism)
            {
                return this.mechanisms.TryGetValue(name, out mechanism);
            }
        }

        class ListenerSaslProfile : SaslProfile
        {
            readonly ConnectionListener listener;
            SaslProfile innerProfile;

            public ListenerSaslProfile(ConnectionListener listener)
            {
                this.listener = listener;
            }

            public IPrincipal GetPrincipal()
            {
                IAuthenticated authenticated = this.innerProfile as IAuthenticated;
                if (authenticated != null)
                {
                    return authenticated.Principal;
                }

                return null;
            }

            protected override ITransport UpgradeTransport(ITransport transport)
            {
                return transport;
            }

            protected override DescribedList GetStartCommand(string hostname)
            {
                Symbol[] symbols = this.listener.saslSettings.Mechanisms;
                return new SaslMechanisms() { SaslServerMechanisms = symbols };
            }

            protected override DescribedList OnCommand(DescribedList command)
            {
                if (this.innerProfile == null)
                {
                    if (command.Descriptor.Code == Codec.SaslInit.Code)
                    {
                        var init = (SaslInit)command;
                        SaslMechanism saslMechanism;
                        if (!this.listener.saslSettings.TryGetMechanism(init.Mechanism, out saslMechanism))
                        {
                            throw new AmqpException(ErrorCode.NotImplemented, init.Mechanism);
                        }

                        this.innerProfile = saslMechanism.CreateProfile();
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
                    host.Equals(Environment.GetEnvironmentVariable("COMPUTERNAME"), StringComparison.OrdinalIgnoreCase) ||
                    host.Equals(Amqp.TaskExtensions.GetHostEntryAsync(string.Empty).Result.HostName, StringComparison.OrdinalIgnoreCase))
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
                    addresses.AddRange(Amqp.TaskExtensions.GetHostAddressesAsync(host).GetAwaiter().GetResult());
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
                            this.listenSockets[i].Dispose();
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
                    socket.Dispose();
                }
            }

            protected virtual Task<IAsyncTransport> CreateTransportAsync(Socket socket)
            {
                var tcs = new TaskCompletionSource<IAsyncTransport>();
                tcs.SetResult(new ListenerTcpTransport(socket, this.Listener.BufferManager));
                return tcs.Task;
            }

            async Task AcceptAsync(Socket socket)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += (s, a) => ((TaskCompletionSource<Socket>)a.UserToken).Complete(a, b => b.AcceptSocket);

                while (!this.closed)
                {
                    try
                    {
                        args.AcceptSocket = null;
                        Socket acceptSocket = await socket.AcceptAsync(args, SocketFlags.None);
                        var task = this.HandleSocketAsync(acceptSocket);
                    }
                    catch (ObjectDisposedException)
                    {
                        // listener is closed
                    }
                    catch (Exception exception)
                    {
                        Trace.WriteLine(TraceLevel.Warning, exception.ToString());
                    }
                }

                args.Dispose();
                socket.Dispose();
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
                SslStream sslStream;
                if (this.Listener.sslSettings == null)
                {
                    sslStream = new SslStream(new NetworkStream(socket));
                    await sslStream.AuthenticateAsServerAsync(this.certificate);
                }
                else
                {
                    sslStream = new SslStream(new NetworkStream(socket), false,
                        this.Listener.sslSettings.RemoteCertificateValidationCallback);

                    await sslStream.AuthenticateAsServerAsync(this.certificate, this.Listener.sslSettings.ClientCertificateRequired,
                        this.Listener.sslSettings.Protocols, this.Listener.sslSettings.CheckCertificateRevocation);
                }

                return new ListenerTcpTransport(sslStream, this.Listener.BufferManager);
            }
        }

        class ListenerTcpTransport : TcpTransport, IAuthenticated
        {
            public ListenerTcpTransport(Socket socket, IBufferManager bufferManager)
                : base(bufferManager)
            {
                this.socketTransport = new TcpSocket(this, socket);
                this.writer = new Writer(this, this.socketTransport);
            }

            public ListenerTcpTransport(SslStream sslStream, IBufferManager bufferManager)
                : base(bufferManager)
            {
                this.socketTransport = new SslSocket(this, sslStream);
                this.writer = new Writer(this, this.socketTransport);
                if (sslStream.RemoteCertificate != null)
                {
                    this.Principal = new GenericPrincipal(
                        new X509Identity(sslStream.RemoteCertificate),
                        new string[0]);
                }
            }

            public IPrincipal Principal
            {
                get;
                private set;
            }
        }

#if NETFX
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
                    var wsTransport = new ListenerWebSocketTransport(wsContext);
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

        class ListenerWebSocketTransport : WebSocketTransport, IAuthenticated
        {
            public ListenerWebSocketTransport(HttpListenerWebSocketContext context)
                : base(context.WebSocket)
            {
                this.Principal = context.User;
            }

            public IPrincipal Principal
            {
                get;
                private set;
            }
        }
#endif
    }
}
