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
    using System.Net.Security;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Amqp.Framing;
    using Amqp.Handler;
    using Amqp.Sasl;

    /// <summary>
    /// The factory to create connections asynchronously.
    /// </summary>
    public partial class ConnectionFactory : ConnectionFactoryBase
    {
        Dictionary<string, TransportProvider> transportFactories;
        SslSettings sslSettings;
        SaslSettings saslSettings;

        /// <summary>
        /// Constructor to create a connection factory with default transport
        /// implementations.
        /// </summary>
        public ConnectionFactory()
            : base()
        {
        }

        /// <summary>
        /// Creates a connection factory with a custom transport factory.
        /// </summary>
        /// <param name="providers">The custom transport providers.</param>
        /// <remarks>The library provides built-in transport implementation for address schemes
        /// "amqp", "amqps" (and "ws", "wss" on .Net framework). Application can replace or
        /// extend it with custom implementations. When the built-in provider is replaced,
        /// the TCP and SSL settings of the connection factory will not be applied to the
        /// custom implementation.
        /// </remarks>
        public ConnectionFactory(IEnumerable<TransportProvider> providers)
            : this()
        {
            this.transportFactories = new Dictionary<string, TransportProvider>(StringComparer.OrdinalIgnoreCase);
            foreach (var provider in providers)
            {
                foreach (var scheme in provider.AddressSchemes)
                {
                    this.transportFactories[scheme] = provider;
                }
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

        internal SslSettings SslInternal
        {
            get { return this.sslSettings; }
        }

        /// <summary>
        /// Creates a new connection with a protocol handler.
        /// </summary>
        /// <param name="address">The address of remote endpoint to connect to.</param>
        /// <param name="handler">The protocol handler.</param>
        /// <returns>A task for the connection creation operation. On success, the result is an AMQP <see cref="Connection"/></returns>
        public Task<Connection> CreateAsync(Address address, IHandler handler)
        {
            return this.CreateAsync(address, null, null, handler);
        }

        /// <summary>
        /// Creates a new connection with a custom open frame and a callback to handle remote open frame.
        /// </summary>
        /// <param name="address">The address of remote endpoint to connect to.</param>
        /// <param name="open">If specified, it is sent to open the connection, otherwise an open frame created from the AMQP settings property is sent.</param>
        /// <param name="onOpened">If specified, it is invoked when an open frame is received from the remote peer.</param>
        /// <returns>A task for the connection creation operation. On success, the result is an AMQP <see cref="Connection"/></returns>
        public Task<Connection> CreateAsync(Address address, Open open = null, OnOpened onOpened = null)
        {
            return this.CreateAsync(address, open, onOpened, null);
        }

        async Task<Connection> CreateAsync(Address address, Open open, OnOpened onOpened, IHandler handler)
        {
            IAsyncTransport transport;
            TransportProvider provider;
            if (this.transportFactories != null && this.transportFactories.TryGetValue(address.Scheme, out provider))
            {
                transport = await provider.CreateAsync(address).ConfigureAwait(false);
            }
            else if (TcpTransport.MatchScheme(address.Scheme))
            {
                TcpTransport tcpTransport = new TcpTransport(this.BufferManager);
                await tcpTransport.ConnectAsync(address, this).ConfigureAwait(false);
                transport = tcpTransport;
            }
#if NETFX
            else if (WebSocketTransport.MatchScheme(address.Scheme))
            {
                WebSocketTransport wsTransport = new WebSocketTransport();
                await wsTransport.ConnectAsync(address, null).ConfigureAwait(false);
                transport = wsTransport;
            }
#endif
            else
            {
                throw new NotSupportedException(address.Scheme);
            }

            try

            {
                if (address.User != null)
                {
                    SaslPlainProfile profile = new SaslPlainProfile(address.User, address.Password);
                    transport = await profile.OpenAsync(address.Host, this.BufferManager, transport, null).ConfigureAwait(false);
                }
                else if (this.saslSettings != null && this.saslSettings.Profile != null)
                {
                    transport = await this.saslSettings.Profile.OpenAsync(address.Host, this.BufferManager, transport, null).ConfigureAwait(false);
                }
            }
            catch
            {
                transport.Close();
                throw;
            }

            AsyncPump pump = new AsyncPump(this.BufferManager, transport);
            Connection connection = new Connection(this.BufferManager, this.AMQP, address, transport, open, onOpened, handler);
            pump.Start(connection);

            return connection;
        }

        /// <summary>
        /// Contains the TLS/SSL settings for a connection.
        /// </summary>
        public class SslSettings
        {
#if NETFX40
            internal const SslProtocols DefaultSslProtocols = SslProtocols.Default;
#else
            internal const SslProtocols DefaultSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
#endif

            internal SslSettings()
            {
                this.Protocols = SslSettings.DefaultSslProtocols;
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

            /// <summary>
            /// Gets or sets a local certificate selection callback to select the certificate which should be used for authentication.
            /// </summary>
            public LocalCertificateSelectionCallback LocalCertificateSelectionCallback
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
    }
}
