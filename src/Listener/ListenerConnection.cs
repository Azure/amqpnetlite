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
    using System.Security.Principal;
    using System.Threading;
    using Amqp.Framing;
    using Amqp.Handler;

    /// <summary>
    /// An AMQP connection used by the listener.
    /// </summary>
    public class ListenerConnection : Connection
    {
        readonly static OnOpened onOpened = OnOpen;
        readonly ConnectionListener listener;

        internal ListenerConnection(ConnectionListener listener, Address address, IHandler handler, IAsyncTransport transport)
            : base(listener.BufferManager, listener.AMQP, address, transport, null, onOpened, handler)
        {
            this.listener = listener;
        }

        /// <summary>
        /// Gets a IPrincipal object for the connection. If the value is null,
        /// the connection is not authenticated.
        /// </summary>
        public IPrincipal Principal
        {
            get;
            internal set;
        }

        internal ConnectionListener Listener
        {
            get { return this.listener; }
        }

        internal string RemoteContainerId
        {
            get;
            private set;
        }

        internal override void OnBegin(ushort remoteChannel, Begin begin)
        {
            this.ValidateChannel(remoteChannel);

            // this sends a begin to the remote peer
            Begin local = new Begin()
            {
                RemoteChannel = remoteChannel,
                IncomingWindow = Session.defaultWindowSize,
                OutgoingWindow = begin.IncomingWindow,
                NextOutgoingId = 0,
                HandleMax = (uint)(this.listener.AMQP.MaxLinksPerSession - 1)
            };

            var session = new ListenerSession(this, local);

            // this updates the local session state
            begin.RemoteChannel = session.Channel;
            base.OnBegin(remoteChannel, begin);
        }

        static void OnOpen(IConnection connection, Open open)
        {
            var thisPtr = (ListenerConnection)connection;
            thisPtr.RemoteContainerId = open.ContainerId;
        }
    }
}
