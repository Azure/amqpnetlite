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
    using Amqp.Types;

    /// <summary>
    /// The callback that is invoked when a begin performative is received from peer.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="begin">The received begin performative.</param>
    public delegate void OnBegin(Session session, Begin begin);

    /// <summary>
    /// The Session class represents an AMQP session.
    /// </summary>
    public class Session : AmqpObject
    {
        enum State
        {
            Start,
            BeginSent,
            BeginReceived,
            Opened,
            EndPipe,
            EndSent,
            EndReceived,
            End
        }

        internal const uint defaultWindowSize = 2048;
        readonly Connection connection;
        readonly OnBegin onBegin;
        readonly ushort channel;
        uint handleMax;
        Link[] localLinks;
        Link[] remoteLinks;
        State state;

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
            this.incomingWindow = defaultWindowSize;
            this.outgoingWindow = begin.IncomingWindow;
            this.incomingDeliveryId = uint.MaxValue;
            this.localLinks = new Link[1];
            this.remoteLinks = new Link[1];
            this.incomingList = new LinkedList();
            this.outgoingList = new LinkedList();
            this.channel = connection.AddSession(this);

            this.state = State.BeginSent;
            this.SendBegin(begin);
        }

        /// <summary>
        /// Gets the connection where the session was created.
        /// </summary>
        public Connection Connection
        {
            get { return this.connection; }
        }

        object ThisLock
        {
            get { return this; }
        }

        internal ushort Channel
        {
            get { return this.channel; }
        }

        internal void Abort(Error error)
        {
            this.Error = error;

            for (int i = 0; i < this.localLinks.Length; i++)
            {
                if (this.localLinks[i] != null)
                {
                    this.localLinks[i].Abort(error);
                }
            }

            this.CancelPendingDeliveries(error);

            if (this.state != State.End)
            {
                this.state = State.End;
                this.NotifyClosed(error);
            }
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
            Delivery current = null;
            lock (this.ThisLock)
            {
                LinkedList deliveryList = role ? this.incomingList : this.outgoingList;
                current = (Delivery)deliveryList.First;
                while (current != null)
                {
                    if (current == delivery)
                    {
                        if (settled)
                        {
                            deliveryList.Remove(current);
                        }

                        break;
                    }

                    current = (Delivery)current.Next;
                }
            }

            if (current != null)
            {
                current.Settled = settled;
                current.State = state;
                Dispose dispose = new Dispose()
                {
                    Role = role,
                    First = current.DeliveryId,
                    Settled = settled,
                    State = state
                };

                this.SendCommand(dispose);
            }
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
            this.connection.SendCommand(this.channel, command);
        }

        internal void OnBegin(ushort remoteChannel, Begin begin)
        {
            lock (this.ThisLock)
            {
                if (this.state == State.BeginSent)
                {
                    this.state = State.Opened;
                }
                else if (this.state == State.EndPipe)
                {
                    this.state = State.EndSent;
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
            this.Error = end.Error;

            lock (this.ThisLock)
            {
                if (this.state == State.EndSent)
                {
                    this.state = State.End;
                }
                else if (this.state == State.Opened)
                {
                    this.SendEnd();
                    this.state = State.End;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "OnEnd", this.state));
                }

                this.OnClose(end.Error);
                this.NotifyClosed(end.Error);
                return true;
            }
        }

        internal void OnCommand(DescribedList command, ByteBuffer buffer)
        {
            Fx.Assert(this.state < State.EndReceived, "Session is ending or ended and cannot receive commands.");
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
                if (this.state == State.End)
                {
                    return true;
                }
                else if (this.state == State.BeginSent)
                {
                    this.state = State.EndPipe;
                }
                else if (this.state == State.Opened)
                {
                    this.state = State.EndSent;
                }
                else if (this.state == State.EndReceived)
                {
                    this.state = State.End;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "Close", this.state));
                }

                this.SendEnd();
                return this.state == State.End;
            }
        }

        internal virtual void OnAttach(Attach attach)
        {
            if (attach.Handle > this.handleMax)
            {
                throw new AmqpException(ErrorCode.NotAllowed,
                    Fx.Format(SRAmqp.AmqpHandleExceeded, this.handleMax + 1));
            }

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
                    int size = (int)Math.Min(count * 2 - 1, this.handleMax) + 1;
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

                if (this.outgoingWindow == 0)
                {
                    return;
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
                    State = transfer.State
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

                        delivery.OnStateChange(dispose.State);
                    }

                    delivery = next;
                }
            }
        }

        void ThrowIfEnded(string operation)
        {
            if (this.state >= State.EndPipe)
            {
                throw new AmqpException(this.Error ??
                    new Error()
                    {
                        Condition = ErrorCode.IllegalState,
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
            this.connection.SendCommand(this.channel, begin);
        }

        void SendEnd()
        {
            this.connection.SendCommand(this.channel, new End());
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
                    transfer.Batchable = true;
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