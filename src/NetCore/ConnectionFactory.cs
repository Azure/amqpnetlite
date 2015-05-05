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
    using Amqp.Sasl;

    public class ConnectionFactory
    {
        internal SaslSettings saslSettings;
        internal AmqpSettings amqpSettings;

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
