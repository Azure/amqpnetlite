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
    using System.Threading.Tasks;
    using Amqp.Framing;
    using Amqp.Handler;
    using Amqp.Sasl;
#if UWP
    using Windows.Networking.Sockets;
    using Windows.Security.Cryptography.Certificates;
#endif

    /// <summary>
    /// The factory to create connections asynchronously.
    /// </summary>
    public partial class ConnectionFactory
    {
        internal SaslSettings saslSettings;
        internal AmqpSettings amqpSettings;

        /// <summary>
        /// Constructor to create a connection factory.
        /// </summary>
        public ConnectionFactory()
        {
            this.amqpSettings = new AmqpSettings()
            {
                MaxFrameSize = (int)Connection.DefaultMaxFrameSize,
                ContainerId = Guid.NewGuid().ToString(),
                IdleTimeout = int.MaxValue,
                MaxSessionsPerConnection = 8
            };
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
        /// <returns>A task for the connection creation operation. On success, the result is an AMQP <see cref="Connection"/></returns>
        public Task<Connection> CreateAsync(Address address)
        {
            return this.CreateAsync(address, null, null, null);
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
        public Task<Connection> CreateAsync(Address address, Open open, OnOpened onOpened)
        {
            return this.CreateAsync(address, open, onOpened, null);
        }

        async Task<Connection> CreateAsync(Address address, Open open, OnOpened onOpened, IHandler handler)
        {
            IAsyncTransport transport;
#if !WINDOWS_PHONE
            if (WebSocketTransport.MatchScheme(address.Scheme))
            {
                WebSocketTransport wsTransport = new WebSocketTransport();
                await wsTransport.ConnectAsync(address).ConfigureAwait(false);
                transport = wsTransport;
            }
            else
#endif
            if (string.Equals(address.Scheme, Address.Amqp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(address.Scheme, Address.Amqps, StringComparison.OrdinalIgnoreCase))
            {
                TcpTransport tcpTransport = new TcpTransport();
                await tcpTransport.ConnectAsync(address, this).ConfigureAwait(false);
                transport = tcpTransport;
            }
            else
            {
                throw new NotSupportedException(address.Scheme);
            }

            if (address.User != null)
            {
                SaslPlainProfile profile = new SaslPlainProfile(address.User, address.Password);
                transport = await profile.OpenAsync(address.Host, null, transport, null).ConfigureAwait(false);
            }
            else if (this.saslSettings != null && this.saslSettings.Profile != null)
            {
                transport = await this.saslSettings.Profile.OpenAsync(address.Host, null, transport, null).ConfigureAwait(false);
            }

            AsyncPump pump = new AsyncPump(null, transport);
            Connection connection = new Connection(null, this.AMQP, address, transport, open, onOpened, handler);
            pump.Start(connection);

            return connection;
        }

#if UWP
        internal SslSettings sslSettings;

        /// <summary>
        /// Gets the TLS/SSL settings on the connection factory.
        /// </summary>
        public SslSettings SSL
        {
            get
            {
                return this.sslSettings ?? (this.sslSettings = new SslSettings());
            }
        }

        /// <summary>
        /// Contains the TLS/SSL settings for a connection.
        /// </summary>
        public class SslSettings
        {
            /// <summary>
            /// Gets or sets the client SSL/TLS certificate that will be sent to the server.
            /// </summary>
            public Certificate ClientCertificate { get; set; }

            /// <summary>
            /// Gets or sets the protection level that represents the integrity and encryption
            /// for the underlying stream socket.
            /// </summary>
            public SocketProtectionLevel ProtectionLevel { get; set; }
        }
#endif

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
