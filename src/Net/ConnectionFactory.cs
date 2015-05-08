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
    using System.Diagnostics;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Amqp.Framing;
    using Amqp.Sasl;

    /// <summary>
    /// The factory to create connections asynchronously.
    /// </summary>
    public class ConnectionFactory
    {
        internal TcpSettings tcpSettings;
        internal SslSettings sslSettings;
        internal SaslSettings saslSettings;
        internal AmqpSettings amqpSettings;

        /// <summary>
        /// Constructor to create a connection factory.
        /// </summary>
        public ConnectionFactory()
        {
            this.tcpSettings = new TcpSettings()
            {
                NoDelay = true
            };

            this.amqpSettings = new AmqpSettings()
            {
                MaxFrameSize = (int)Connection.DefaultMaxFrameSize,
                ContainerId = Process.GetCurrentProcess().ProcessName,
                IdleTimeout = int.MaxValue,
                MaxSessionsPerConnection = 8
            };
        }

        /// <summary>
        /// Gets the TCP settings on the factory.
        /// </summary>
        public TcpSettings TCP
        {
            get
            {
                return this.tcpSettings ?? (this.tcpSettings = new TcpSettings());
            }
        }

        /// <summary>
        /// Gets the TLS/SSL settings on the factory.
        /// </summary>
        public SslSettings SSL
        {
            get
            {
                return this.sslSettings ?? (this.sslSettings = new SslSettings());
            }
        }

        /// <summary>
        /// Gets the SASL settings on the factory.
        /// </summary>
        public SaslSettings SASL
        {
            get
            {
                return this.saslSettings ?? (this.saslSettings = new SaslSettings());
            }
        }

        /// <summary>
        /// Gets the AMQP settings on the factory.
        /// </summary>
        public AmqpSettings AMQP
        {
            get { return this.amqpSettings; }
        }

        /// <summary>
        /// Creates a new connection.
        /// </summary>
        /// <param name="address">The address of remote endpoint to connect to.</param>
        /// <returns></returns>
        public Task<Connection> CreateAsync(Address address)
        {
            return this.CreateAsync(address, null, null);
        }

        /// <summary>
        /// Creates a new connection with a custom open frame and a callback to handle remote open frame.
        /// </summary>
        /// <param name="address">The address of remote endpoint to connect to.</param>
        /// <param name="open">If specified, it is sent to open the connection, otherwise an open frame created from the AMQP settings property is sent.</param>
        /// <param name="onOpened">If specified, it is invoked when an open frame is received from the remote peer.</param>
        /// <returns></returns>
        public async Task<Connection> CreateAsync(Address address, Open open, OnOpened onOpened)
        {
            IAsyncTransport transport;
            if (WebSocketTransport.MatchScheme(address.Scheme))
            {
                WebSocketTransport wsTransport = new WebSocketTransport();
                await wsTransport.ConnectAsync(address);
                transport = wsTransport;
            }
            else
            {
                TcpTransport tcpTransport = new TcpTransport();
                await tcpTransport.ConnectAsync(address, this);
                transport = tcpTransport;
            }

            if (address.User != null)
            {
                SaslPlainProfile profile = new SaslPlainProfile(address.User, address.Password);
                transport = await profile.OpenAsync(address.Host, transport);
            }
            else if (this.saslSettings != null && this.saslSettings.Profile != null)
            {
                transport = await this.saslSettings.Profile.OpenAsync(address.Host, transport);
            }

            AsyncPump pump = new AsyncPump(transport);
            Connection connection = new Connection(this, address, transport, open, onOpened);
            pump.Start(connection);

            return connection;
        }

        /// <summary>
        /// Contains the TCP settings for a connection.
        /// </summary>
        public class TcpSettings
        {
            const int DefaultBufferSize = 8192;
            bool? noDelay;
            int? receiveBufferSize;
            int? receiveTimeout;
            int? sendBufferSize;
            int? sendTimeout;

            /// <summary>
            /// Specifies the LingerOption option of the TCP socket.
            /// </summary>
            public LingerOption LingerOption
            {
                get;
                set;
            }

            /// <summary>
            /// Specifies the NoDelay option of the TCP socket.
            /// </summary>
            public bool NoDelay
            {
                get { return this.noDelay ?? false; }
                set { this.noDelay = value; }
            }

            /// <summary>
            /// Specifies the ReceiveBufferSize option of the TCP socket.
            /// </summary>
            public int ReceiveBufferSize
            {
                get { return this.receiveBufferSize ?? DefaultBufferSize; }
                set { this.receiveBufferSize = value; }
            }

            /// <summary>
            /// Specifies the ReceiveTimeout option of the TCP socket.
            /// </summary>
            public int ReceiveTimeout
            {
                get { return this.receiveTimeout ?? 0; }
                set { this.receiveTimeout = value; }
            }

            /// <summary>
            /// Specifies the SendBufferSize option of the TCP socket.
            /// </summary>
            public int SendBufferSize
            {
                get { return this.sendBufferSize ?? DefaultBufferSize; }
                set { this.sendBufferSize = value; }
            }

            /// <summary>
            /// Specifies the SendTimeout option of the TCP socket.
            /// </summary>
            public int SendTimeout
            {
                get { return this.sendTimeout ?? 0; }
                set { this.sendTimeout = value; }
            }

            internal void Configure(Socket socket)
            {
                if (this.noDelay != null) socket.NoDelay = this.noDelay.Value;
                if (this.receiveBufferSize != null) socket.ReceiveBufferSize = this.receiveBufferSize.Value;
                if (this.receiveTimeout != null) socket.ReceiveTimeout = this.receiveTimeout.Value;
                if (this.sendBufferSize != null) socket.SendBufferSize = this.sendBufferSize.Value;
                if (this.sendTimeout != null) socket.SendTimeout = this.sendTimeout.Value;
                if (this.LingerOption != null) socket.LingerState = this.LingerOption;
            }
        }

        /// <summary>
        /// Contains the TLS/SSL settings for a connection.
        /// </summary>
        public class SslSettings
        {
            internal SslSettings()
            {
                this.Protocols = SslProtocols.Default;
                this.ClientCertificates = new X509CertificateCollection();
            }

            /// <summary>
            /// Client certificates to use for mutual authentication.
            /// </summary>
            public X509CertificateCollection ClientCertificates
            {
                get;
                set;
            }

            /// <summary>
            /// Supported protocols to use.
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
            /// <summary>
            /// The SASL profile to use for SASL negotiation.
            /// </summary>
            public SaslProfile Profile
            {
                get;
                set;
            }
        }

        /// <summary>
        /// Contains the AMQP settings for a connection.
        /// </summary>
        public class AmqpSettings
        {
            /// <summary>
            /// Gets or sets the open.max-frame-size field.
            /// </summary>
            public int MaxFrameSize
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the open.container-id field.
            /// </summary>
            public string ContainerId
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the open.hostname field.
            /// </summary>
            public string HostName
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the open.channel-max field.
            /// </summary>
            public ushort MaxSessionsPerConnection
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the open.idle-time-out field.
            /// </summary>
            public int IdleTimeout
            {
                get;
                set;
            }
        }
    }
}
