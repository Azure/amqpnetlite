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

    public class ConnectionFactory
    {
        internal TcpSettings tcpSettings;
        internal SslSettings sslSettings;
        internal SaslSettings saslSettings;
        internal AmqpSettings amqpSettings;

        public ConnectionFactory()
        {
            this.amqpSettings = new AmqpSettings()
            {
                MaxFrameSize = (int)Connection.DefaultMaxFrameSize,
                ContainerId = Process.GetCurrentProcess().ProcessName,
                IdleTimeout = int.MaxValue,
                MaxSessionsPerConnection = 8
            };
        }

        public TcpSettings TCP
        {
            get
            {
                return this.tcpSettings ?? (this.tcpSettings = new TcpSettings());
            }
        }

        public SslSettings SSL
        {
            get
            {
                return this.sslSettings ?? (this.sslSettings = new SslSettings());
            }
        }

        public SaslSettings SASL
        {
            get
            {
                return this.saslSettings ?? (this.saslSettings = new SaslSettings());
            }
        }

        public AmqpSettings AMQP
        {
            get { return this.amqpSettings; }
        }

        public Task<Connection> CreateAsync(Address address)
        {
            return this.CreateAsync(address, null, null);
        }

        public Task<Connection> CreateAsync(Address address, OnOpened onOpened)
        {
            return this.CreateAsync(address, null, onOpened);
        }

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

        public class TcpSettings
        {
            const int DefaultBufferSize = 8192;
            bool? noDelay;
            int? receiveBufferSize;
            int? receiveTimeout;
            int? sendBufferSize;
            int? sendTimeout;

            public LingerOption LingerOption
            {
                get;
                set;
            }

            public bool NoDelay
            {
                get { return this.noDelay ?? false; }
                set { this.noDelay = value; }
            }

            public int ReceiveBufferSize
            {
                get { return this.receiveBufferSize ?? DefaultBufferSize; }
                set { this.receiveBufferSize = value; }
            }

            public int ReceiveTimeout
            {
                get { return this.receiveTimeout ?? 0; }
                set { this.receiveTimeout = value; }
            }

            public int SendBufferSize
            {
                get { return this.sendBufferSize ?? DefaultBufferSize; }
                set { this.sendBufferSize = value; }
            }

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

        public class SslSettings
        {
            public SslSettings()
            {
                this.Protocols = SslProtocols.Default;
                this.ClientCertificates = new X509CertificateCollection();
            }

            public X509CertificateCollection ClientCertificates
            {
                get;
                set;
            }

            public SslProtocols Protocols
            {
                get;
                set;
            }

            public bool CheckCertificateRevocation
            {
                get;
                set;
            }

            public RemoteCertificateValidationCallback RemoteCertificateValidationCallback
            {
                get;
                set;
            }
        }

        public class SaslSettings
        {
            public SaslProfile Profile
            {
                get;
                set;
            }
        }

        public class AmqpSettings
        {
            public int MaxFrameSize
            {
                get;
                set;
            }

            public string ContainerId
            {
                get;
                set;
            }

            public string HostName
            {
                get;
                set;
            }

            public ushort MaxSessionsPerConnection
            {
                get;
                set;
            }

            public int IdleTimeout
            {
                get;
                set;
            }
        }
    }
}
