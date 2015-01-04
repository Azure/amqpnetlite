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
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Amqp.Framing;
    using Amqp.Sasl;
    using Amqp.Types;
    using System.Security.Cryptography.X509Certificates;
    using System.Net.Security;

    public class ConnectionListener : ConnectionFactory
    {
        readonly IContainer container;
        readonly TransportListener listener;
        readonly SaslMechanism[] saslMechanisms;
        readonly HashSet<Connection> connections;
        readonly Address address;

        public ConnectionListener(Uri address, string sslValue, string userInfo, IContainer container)
        {
            this.connections = new HashSet<Connection>();
            this.saslMechanisms = this.CreateSaslMechanisms(userInfo);
            this.container = container;
            this.address = new Address(address.Host, address.Port, null, null, "/", address.Scheme);
            if (address.Scheme.Equals("amqp", StringComparison.OrdinalIgnoreCase))
            {
                this.listener = new TcpTransportListener(this, address.Host, address.Port);
            }
            else if (address.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase))
            {
                this.listener = new TlsTransportListener(this, address.Host, address.Port, sslValue);
            }
            else
            {
                throw new NotSupportedException(address.Scheme);
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

        SaslMechanism[] CreateSaslMechanisms(string userInfo)
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

        async Task HandleTransportAsync(Socket socket)
        {
            IAsyncTransport transport;
            try
            {
                transport = await this.listener.CreateTransportAsync(socket);
            }
            catch (Exception exception)
            {
                Trace.WriteLine(TraceLevel.Error, exception.ToString());
                socket.Close();
                return;
            }

            if (this.saslMechanisms != null)
            {
                try
                {
                    ListenerSasProfile profile = new ListenerSasProfile(this);
                    await profile.OpenAsync(null, transport);
                }
                catch (Exception exception)
                {
                    Trace.WriteLine(TraceLevel.Error, exception.ToString());
                    transport.Close();
                    return;
                }

                transport = new AsyncSaslTransport(transport);
            }

            Connection connection = new ListenerConnection(this, this.address, transport);
            connection.OnClosed += this.OnConnectionClosed;
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

            protected override DescribedList GetStartCommand(string hostname)
            {
                Symbol[] symbols = new Symbol[this.listener.saslMechanisms.Length];
                for (int i = 0; i < symbols.Length; i++)
                {
                    symbols[i] = this.listener.saslMechanisms[i].Name;
                }

                return new SaslMechanisms() { SaslServerMechanisms = Multiple.From(symbols) };
            }

            internal override DescribedList OnCommand(DescribedList command)
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

                return this.innerProfile.OnCommand(command);
            }
        }

        abstract class TransportListener
        {
            protected ConnectionListener Listener
            {
                get;
                set;
            }

            public abstract void Open();

            public abstract void Close();

            public abstract Task<IAsyncTransport> CreateTransportAsync(Socket socket);
        }

        class TcpTransportListener : TransportListener
        {
            Socket[] listenSockets;
            bool closed;

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

            public override Task<IAsyncTransport> CreateTransportAsync(Socket socket)
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

                        if (this.Listener.tcpSettings != null)
                        {
                            this.Listener.tcpSettings.Configure(acceptSocket);
                        }

                        var t = this.Listener.HandleTransportAsync(acceptSocket);
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

            public TlsTransportListener(ConnectionListener listener, string host, int port, string sslValue)
                : base(listener, host, port)
            {
                this.certificate = GetCertificate(sslValue);
            }

            public override async Task<IAsyncTransport> CreateTransportAsync(Socket socket)
            {
                var sslStream = new SslStream(new NetworkStream(socket));
                await sslStream.AuthenticateAsServerAsync(this.certificate);
                return new TcpTransport(sslStream);
            }
        }

        static X509Certificate2 GetCertificate(string certFindValue)
        {
            StoreLocation[] locations = new StoreLocation[] { StoreLocation.LocalMachine, StoreLocation.CurrentUser };
            foreach (StoreLocation location in locations)
            {
                X509Store store = new X509Store(StoreName.My, location);
                store.Open(OpenFlags.OpenExistingOnly);

                X509Certificate2Collection collection = store.Certificates.Find(
                    X509FindType.FindBySubjectName,
                    certFindValue,
                    false);

                if (collection.Count == 0)
                {
                    collection = store.Certificates.Find(
                        X509FindType.FindByThumbprint,
                        certFindValue,
                        false);
                }

                store.Close();

                if (collection.Count > 0)
                {
                    return collection[0];
                }
            }

            throw new ArgumentException("No certificate can be found using the find value " + certFindValue);
        }
    }
}
