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

namespace Amqp.Handler
{
    using Amqp.Framing;

    /// <summary>
    /// Identifies a protocol event.
    /// </summary>
    public enum EventId
    {
        /// <summary>
        /// An <see cref="Open"/> performative (<see cref="Event.Context"/>) is about to send. 
        /// </summary>
        ConnectionLocalOpen,
        /// <summary>
        /// An <see cref="Open"/> performative (<see cref="Event.Context"/>) is received. 
        /// </summary>
        ConnectionRemoteOpen,
        /// <summary>
        /// A <see cref="Close"/> performative (<see cref="Event.Context"/>) is about to send. 
        /// </summary>
        ConnectionLocalClose,
        /// <summary>
        /// A <see cref="Close"/> performative (<see cref="Event.Context"/>) is received. 
        /// </summary>
        ConnectionRemoteClose,
        /// <summary>
        /// A <see cref="Begin"/> performative (<see cref="Event.Context"/>) is about to send. 
        /// </summary>
        SessionLocalOpen,
        /// <summary>
        /// A <see cref="Begin"/> performative (<see cref="Event.Context"/>) is received. 
        /// </summary>
        SessionRemoteOpen,
        /// <summary>
        /// An <see cref="End"/> performative (<see cref="Event.Context"/>) is about to send. 
        /// </summary>
        SessionLocalClose,
        /// <summary>
        /// An <see cref="End"/> performative (<see cref="Event.Context"/>) is received.
        /// </summary>
        SessionRemoteClose,
        /// <summary>
        /// An <see cref="Attach"/> performative (<see cref="Event.Context"/>) is about to send. 
        /// </summary>
        LinkLocalOpen,
        /// <summary>
        /// An <see cref="Attach"/> performative (<see cref="Event.Context"/>) is received.
        /// </summary>
        LinkRemoteOpen,
        /// <summary>
        /// A <see cref="Detach"/> performative (<see cref="Event.Context"/>) is about to send. 
        /// </summary>
        LinkLocalClose,
        /// <summary>
        /// A <see cref="Detach"/> performative (<see cref="Event.Context"/>) is received.
        /// </summary>
        LinkRemoteClose,
        /// <summary>
        /// A <see cref="IDelivery"/> (<see cref="Event.Context"/>) is about to send.
        /// </summary>
        SendDelivery,
        /// <summary>
        /// A <see cref="IDelivery"/> (<see cref="Event.Context"/>) is received.
        /// </summary>
        ReceiveDelivery,
    }

    /// <summary>
    /// Represents a protocol event.
    /// </summary>
    public struct Event
    {
        /// <summary>
        /// Gets the event identifier.
        /// </summary>
        public EventId Id { get; private set; }

        /// <summary>
        /// Gets the connection where the event occurs.
        /// </summary>
        public Connection Connection { get; private set; }

        /// <summary>
        /// Gets the session, if available, where the event occurs.
        /// </summary>
        public Session Session { get; private set; }

        /// <summary>
        /// Gets the link, if available, where the event occurs.
        /// </summary>
        public Link Link { get; private set; }

        /// <summary>
        /// Gets the context associated with the event as defined in each <see cref="EventId"/>.
        /// </summary>
        public object Context { get; private set; }

        internal static Event Create(EventId id, Connection connection = null, Session session = null, Link link = null, object context = null)
        {
            return new Event() { Id = id, Connection = connection, Session = session, Link = link, Context = context };
        }
    }
}
