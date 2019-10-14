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
    using Amqp.Framing;
    using Amqp.Handler;
    using Amqp.Types;
    
    /// <summary>
    /// The state of a session.
    /// </summary>
    public enum SessionState
    {
        /// <summary>
        /// The session is started.
        /// </summary>
        Start,
        
        /// <summary>
        /// Begin frame was sent.
        /// </summary>
        BeginSent,
        
        /// <summary>
        /// Begin frame was received.
        /// </summary>
        BeginReceived,
        
        /// <summary>
        /// The session is opened. 
        /// </summary>
        Opened,
        
        /// <summary>
        /// End frame received.
        /// </summary>
        EndReceived,
        
        /// <summary>
        /// End frame was sent.
        /// </summary> 
        EndSent,
        
        /// <summary>
        /// The session is closing.
        /// </summary>
        EndPipe,
        
        /// <summary>
        /// The session is closed.
        /// </summary>
        End
    }

    /// <summary>
    /// The Session class represents an AMQP session.
    /// </summary>
    public partial class Session : AmqpObject
    {
        internal const uint defaultWindowSize = 2048;
        readonly Connection connection;
        readonly OnBegin onBegin;
        readonly ushort channel;
        uint handleMax;
        Link[] localLinks;
        Link[] remoteLinks;
        SessionState state;
        private readonly object lockObject = new object();


        // incoming flow control
        SequenceNumber incomingDeliveryId;
        LinkedList incomingList;
        SequenceNumber nextIncomingId;
        uint incomingWindow;

        // outgoing delivery tracking & flow control
        SequenceNumber outgoingDeliveryId;
        LinkedList outgoingList;
        SequenceNumber nextOutgoingId;
        uint outgoingWindow;

        /// <summary>
        /// Initializes a session object.
        /// </summary>
        /// <param name="connection">The connection within which to create the session.</param>
        public Session(Connection connection)
            : this(connection, Default(connection), null)
        {
        }

        /// <summary>
        /// Initializes a session object with a custom Begin performative.
        /// </summary>
        /// <param name="connection">The connection in which the session will be created.</param>
        /// <param name="begin">The Begin performative to be sent to the remote peer.</param>
        /// <param name="onBegin">The callback to invoke when a begin is received from peer.</param>
        public Session(Connection connection, Begin begin, OnBegin onBegin)
        {
            this.connection = connection;
            this.onBegin = onBegin;
            this.handleMax = begin.HandleMax;
            this.nextOutgoingId = begin.NextOutgoingId;
            this.incomingWindow = begin.IncomingWindow;
            this.outgoingWindow = begin.OutgoingWindow;
            this.incomingDeliveryId = uint.MaxValue;
            this.localLinks = new Link[1];
            this.remoteLinks = new Link[1];
            this.incomingList = new LinkedList();
            this.outgoingList = new LinkedList();
            this.channel = connection.AddSession(this);

            this.state = SessionState.BeginSent;
            this.SendBegin(begin);
        }

        /// <summary>
        /// Gets the connection where the session was created.
        /// </summary>
        public Connection Connection
        {
            get { return this.connection; }
        }

        /// <summary>
        /// Get the session state. 
        /// </summary>
        public SessionState SessionState
        {
            get { return state; }
        }

        object ThisLock
        {
            get { return this.lockObject; }
        }

        internal ushort Channel
        {
            get { return this.channel; }
        }

        internal void Abort(Error error)
        {
            this.CloseCalled = true;
            this.Error = error;
            this.AbortLinks(error);

            if (this.state != SessionState.End)
            {
                this.state = SessionState.End;
                this.NotifyClosed(error);
            }
        }

        void AbortLinks(Error error)
        {
            for (int i = 0; i < this.localLinks.Length; i++)
            {
                if (this.localLinks[i] != null)
                {
                    this.localLinks[i].Abort(error, "session ended");
                }
            }

            this.CancelPendingDeliveries(error);
        }

        internal uint AddLink(Link link)
        {
            this.ThrowIfEnded("AddLink");
            lock (this.ThisLock)
            {
                int index = -1;
                int count = this.localLinks.Length;
                for (int i = 0; i < count; ++i)
                {
                    if (this.localLinks[i] == null)
                    {
                        if (index < 0)
                        {
                            index = i;
                        }
                    }
                    else if (string.Compare(this.localLinks[i].Name, link.Name) == 0)
                    {
                        throw new AmqpException(ErrorCode.NotAllowed, link.Name + " has been attached.");
                    }
                }

                if (index >= 0)
                {
                    this.localLinks[index] = link;
                    return (uint)index;
                }

                if (count - 1 < this.handleMax)
                {
                    int size = (int)Math.Min(count * 2 - 1, this.handleMax) + 1;
                    Link[] expanded = new Link[size];
                    Array.Copy(this.localLinks, expanded, count);
                    this.localLinks = expanded;
                    this.localLinks[count] = link;
                    return (ushort)count;
                }

                throw new AmqpException(ErrorCode.NotAllowed,
                    Fx.Format(SRAmqp.AmqpHandleExceeded, this.handleMax + 1));
            }
        }

        internal void RemoveLink(Link link, uint remoteHandle)
        {
            lock (this.ThisLock)
            {
                this.localLinks[link.Handle] = null;
                this.remoteLinks[remoteHandle] = null;
            }
        }

        internal void SendDelivery(Delivery delivery)
        {
            lock (this.ThisLock)
            {
                this.ThrowIfEnded("Send");
                this.outgoingList.Add(delivery);
                this.WriteDelivery(delivery);
            }
        }

        internal void DisposeDelivery(bool role, Delivery delivery, DeliveryState state, bool settled)
        {
            if (delivery.Settled)
            {
                return;
            }

            delivery.Settled = settled;
            delivery.State = state;
            if (settled)
            {
                lock (this.ThisLock)
                {
                    LinkedList deliveryList = role ? this.incomingList : this.outgoingList;
                    deliveryList.Remove(delivery);
                }
            }

            Dispose dispose = new Dispose()
            {
                Role = role,
                First = delivery.DeliveryId,
                Settled = settled,
                State = state
            };

            this.SendCommand(dispose);
        }

        internal void SendFlow(Flow flow)
        {
            lock (this.ThisLock)
            {
                this.incomingWindow = defaultWindowSize;
                flow.NextOutgoingId = this.nextOutgoingId;
                flow.OutgoingWindow = this.outgoingWindow;
                flow.NextIncomingId = this.nextIncomingId;
                flow.IncomingWindow = this.incomingWindow;

                this.SendCommand(flow);
            }
        }

        internal void SendCommand(DescribedList command)
        {
            if (command.Descriptor.Code == Codec.End.Code || this.state < SessionState.EndSent)
            {
                this.connection.SendCommand(this.channel, command);
            }
        }

        internal void OnBegin(ushort remoteChannel, Begin begin)
        {
            IHandler handler = this.connection.Handler;
            if (handler != null && handler.CanHandle(EventId.SessionRemoteOpen))
            {
                handler.Handle(Event.Create(EventId.SessionRemoteOpen, this.connection, this, context: begin));
            }

            lock (this.ThisLock)
            {
                if (this.state == SessionState.BeginSent)
                {
                    this.state = SessionState.Opened;
                }
                else if (this.state == SessionState.EndPipe)
                {
                    this.state = SessionState.EndSent;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "OnBegin", this.state));
                }

                this.outgoingWindow = begin.IncomingWindow;
                this.nextIncomingId = begin.NextOutgoingId;
            }

            if (begin.HandleMax < this.handleMax)
            {
                this.handleMax = begin.HandleMax;
            }

            if (this.onBegin != null)
            {
                this.onBegin(this, begin);
            }
        }

        internal bool OnEnd(End end)
        {
            IHandler handler = this.connection.Handler;
            if (handler != null && handler.CanHandle(EventId.SessionRemoteClose))
            {
                handler.Handle(Event.Create(EventId.SessionRemoteClose, this.connection, this, context: end));
            }

            this.Error = end.Error;

            lock (this.ThisLock)
            {
                if (this.state == SessionState.EndSent)
                {
                    this.state = SessionState.End;
                }
                else if (this.state == SessionState.Opened)
                {
                    this.SendEnd();
                    this.state = SessionState.End;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "OnEnd", this.state));
                }

                this.AbortLinks(end.Error);
                this.NotifyClosed(end.Error);

                return true;
            }
        }

        internal void OnCommand(DescribedList command, ByteBuffer buffer)
        {
            Fx.Assert(this.state != SessionState.EndReceived && this.state != SessionState.End,
                "Session is ending or ended and cannot receive commands.");
            if (command.Descriptor.Code == Codec.Attach.Code)
            {
                this.OnAttach((Attach)command);
            }
            else if (command.Descriptor.Code == Codec.Detach.Code)
            {
                this.OnDetach((Detach)command);
            }
            else if (command.Descriptor.Code == Codec.Flow.Code)
            {
                this.OnFlow((Flow)command);
            }
            else if (command.Descriptor.Code == Codec.Transfer.Code)
            {
                this.OnTransfer((Transfer)command, buffer);
            }
            else if (command.Descriptor.Code == Codec.Dispose.Code)
            {
                this.OnDispose((Dispose)command);
            }
            else
            {
                throw new AmqpException(ErrorCode.NotImplemented,
                    Fx.Format(SRAmqp.AmqpOperationNotSupported, command.Descriptor.Name));
            }
        }

        /// <summary>
        /// Closes the session.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <returns></returns>
        protected override bool OnClose(Error error)
        {
            this.CancelPendingDeliveries(error);

            lock (this.ThisLock)
            {
                if (this.state == SessionState.End)
                {
                    return true;
                }
                else if (this.state == SessionState.BeginSent)
                {
                    this.state = SessionState.EndPipe;
                }
                else if (this.state == SessionState.Opened)
                {
                    this.state = SessionState.EndSent;
                }
                else if (this.state == SessionState.EndReceived)
                {
                    this.state = SessionState.End;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "Close", this.state));
                }

                this.SendEnd();
                return this.state == SessionState.End;
            }
        }

        internal virtual void OnAttach(Attach attach)
        {
            this.ValidateHandle(attach.Handle);

            Link link = null;
            lock (this.ThisLock)
            {
                for (int i = 0; i < this.localLinks.Length; ++i)
                {
                    Link temp = this.localLinks[i];
                    if (temp != null && string.Compare(temp.Name, attach.LinkName) == 0)
                    {
                        link = temp;
                        this.AddRemoteLink(attach.Handle, link);
                        break;
                    }
                }
            }

            if (link == null)
            {
                throw new AmqpException(ErrorCode.NotFound,
                    Fx.Format(SRAmqp.LinkNotFound, attach.LinkName));
            }

            link.OnAttach(attach.Handle, attach);
        }

        internal void AddRemoteLink(uint remoteHandle, Link link)
        {
            lock (this.ThisLock)
            {
                int count = this.remoteLinks.Length;
                if (count - 1 < remoteHandle)
                {
                    int size = count * 2;
                    while (size - 1 < remoteHandle)
                    {
                        size *= 2;
                    }

                    Link[] expanded = new Link[size];
                    Array.Copy(this.remoteLinks, expanded, count);
                    this.remoteLinks = expanded;
                }

                var remoteLink = this.remoteLinks[remoteHandle];
                if (remoteLink != null)
                {
                    throw new AmqpException(ErrorCode.HandleInUse,
                        Fx.Format(SRAmqp.AmqpHandleInUse, remoteHandle, remoteLink.Name));
                }

                this.remoteLinks[remoteHandle] = link;
            }
        }

        static Begin Default(Connection connection)
        {
            return new Begin()
            {
                IncomingWindow = defaultWindowSize,
                OutgoingWindow = defaultWindowSize,
                HandleMax = (uint)(connection.MaxLinksPerSession - 1),
                NextOutgoingId = uint.MaxValue - 2u
            };
        }

        internal void ValidateHandle(uint handle)
        {
            if (handle > this.handleMax)
            {
                throw new AmqpException(ErrorCode.NotAllowed,
                    Fx.Format(SRAmqp.AmqpHandleExceeded, this.handleMax + 1));
            }
        }

        internal Delivery RemoveDeliveries(Link link)
        {
            LinkedList list = null;
            lock (this.ThisLock)
            {
                Delivery temp = (Delivery)this.outgoingList.First;
                while (temp != null)
                {
                    Delivery curr = temp;
                    temp = (Delivery)temp.Next;
                    if (curr.Link == link)
                    {
                        this.outgoingList.Remove(curr);
                        if (list == null)
                        {
                            list = new LinkedList();
                        }

                        list.Add(curr);
                    }
                }
            }

            return list == null ? null : (Delivery)list.First;
        }

        void CancelPendingDeliveries(Error error)
        {
            Delivery toRealse;
            lock (this.ThisLock)
            {
                toRealse = (Delivery)this.outgoingList.Clear();
            }

            Delivery.ReleaseAll(toRealse, error);
        }

        void OnDetach(Detach detach)
        {
            Link link = this.GetLink(detach.Handle);
            link.OnDetach(detach);
        }

        void OnFlow(Flow flow)
        {
            lock (this.ThisLock)
            {
                this.outgoingWindow = (uint)((flow.NextIncomingId + flow.IncomingWindow) - this.nextOutgoingId);
                if (this.outgoingWindow > 0)
                {
                    Delivery delivery = (Delivery)this.outgoingList.First;
                    while (delivery != null)
                    {
                        if (delivery.Buffer != null && delivery.Buffer.Length > 0)
                        {
                            break;
                        }

                        delivery = (Delivery)delivery.Next;
                    }

                    if (delivery != null)
                    {
                        this.WriteDelivery(delivery);
                    }
                }
            }

            if (flow.HasHandle)
            {
                this.GetLink(flow.Handle).OnFlow(flow);
            }
        }

        void OnTransfer(Transfer transfer, ByteBuffer buffer)
        {
            bool newDelivery;
            lock (this.ThisLock)
            {
                this.nextIncomingId++;
                this.incomingWindow--;
                if (this.incomingWindow == 0)
                {
                    this.SendFlow(new Flow());
                }

                newDelivery = transfer.HasDeliveryId && transfer.DeliveryId > this.incomingDeliveryId;
                if (newDelivery)
                {
                    this.incomingDeliveryId = transfer.DeliveryId;
                }
            }

            Link link = this.GetLink(transfer.Handle);
            Delivery delivery = null;
            if (newDelivery)
            {
                delivery = new Delivery()
                {
                    DeliveryId = transfer.DeliveryId,
                    Link = link,
                    Tag = transfer.DeliveryTag,
                    Settled = transfer.Settled,
                    State = transfer.State,
                    Batchable = transfer.Batchable
                };

                if (!delivery.Settled)
                {
                    lock (this.ThisLock)
                    {
                        this.incomingList.Add(delivery);
                    }
                }
            }

            link.OnTransfer(delivery, transfer, buffer);
        }
        
        void OnDispose(Dispose dispose)
        {
            SequenceNumber first = dispose.First;
            SequenceNumber last = dispose.Last;

            var disposedDeliveries = new List();
            lock (this.ThisLock)
            {
                LinkedList linkedList = dispose.Role ? this.outgoingList : this.incomingList;
                Delivery delivery = (Delivery)linkedList.First;
                while (delivery != null && delivery.DeliveryId <= last)
                {
                    Delivery next = (Delivery)delivery.Next;

                    if (delivery.DeliveryId >= first)
                    {
                        delivery.Settled = dispose.Settled;
                        if (delivery.Settled)
                        {
                            linkedList.Remove(delivery);
                        }

                        disposedDeliveries.Add(delivery);
                    }

                    delivery = next;
                }
            }

            // Update the state of the disposed deliveries
            // Note that calling delivery.OnStateChange may complete some pending SendTask, thereby triggering the
            // execution of some continuations. To avoid any deadlock, this MUST be done outside of any
            // locks (cf issue https://github.com/Azure/amqpnetlite/issues/287).
            for (int i = 0; i < disposedDeliveries.Count; i++)
            {
                var delivery = (Delivery)disposedDeliveries[i];
                disposedDeliveries[i] = null;   // Avoid trailing reference
                delivery.OnStateChange(dispose.State);
            }
        }

        void ThrowIfEnded(string operation)
        {
            if (this.state >= SessionState.EndSent)
            {
                throw new AmqpException(this.Error ??
                    new Error(ErrorCode.IllegalState)
                    {
                        Description = Fx.Format(SRAmqp.AmqpIllegalOperationState, operation, this.state)
                    });
            }
        }

        Link GetLink(uint remoteHandle)
        {
            lock (this.ThisLock)
            {
                Link link = null;
                if (remoteHandle < this.remoteLinks.Length)
                {
                    link = this.remoteLinks[remoteHandle];
                }

                if (link == null)
                {
                    throw new AmqpException(ErrorCode.NotFound,
                        Fx.Format(SRAmqp.AmqpHandleNotFound, remoteHandle, this.channel));
                }

                return link;
            }
        }

        void SendBegin(Begin begin)
        {
            IHandler handler = this.connection.Handler;
            if (handler != null && handler.CanHandle(EventId.SessionLocalOpen))
            {
                handler.Handle(Event.Create(EventId.SessionLocalOpen, this.connection, this, context: begin));
            }

            this.connection.SendCommand(this.channel, begin);
        }

        void SendEnd()
        {
            End end = new End();
            IHandler handler = this.connection.Handler;
            if (handler != null && handler.CanHandle(EventId.SessionLocalClose))
            {
                handler.Handle(Event.Create(EventId.SessionLocalClose, this.connection, this, context: end));
            }

            this.connection.SendCommand(this.channel, end);
        }

        void WriteDelivery(Delivery delivery)
        {
            // Must be called under lock. Delivery must be on list already
            while (this.outgoingWindow > 0 && delivery != null)
            {
                this.outgoingWindow--;
                this.nextOutgoingId++;
                Transfer transfer = new Transfer() { Handle = delivery.Handle };

                bool first = delivery.BytesTransfered == 0;
                if (first)
                {
                    // initialize properties for first transfer
                    delivery.DeliveryId = this.outgoingDeliveryId++;
                    transfer.DeliveryTag = delivery.Tag;
                    transfer.DeliveryId = delivery.DeliveryId;
                    transfer.State = delivery.State;
                    transfer.MessageFormat = 0;
                    transfer.Settled = delivery.Settled;
                    transfer.Batchable = delivery.Batchable;
                }

                int len = this.connection.SendCommand(this.channel, transfer, first,
                    delivery.Buffer, delivery.ReservedBufferSize);
                delivery.BytesTransfered += len;

                if (delivery.Buffer.Length == 0)
                {
                    delivery.Buffer.ReleaseReference();
                    Delivery next = (Delivery)delivery.Next;
                    if (delivery.Settled)
                    {
                        this.outgoingList.Remove(delivery);
                    }

                    delivery = next;
                }
            }
        }
    }
}