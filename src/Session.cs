﻿//  ------------------------------------------------------------------------------------
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

#if SMALL_MEMORY
        const int DefaultMaxLinks = 2;
        const uint defaultWindowSize = 256;
        const int MaxLinks = 2;
#else
        const int DefaultMaxLinks = 64;
        const uint defaultWindowSize = 2048;
        const int MaxLinks = 8;
#endif
        readonly Connection connection;
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
            : this(connection, new Begin() { IncomingWindow = defaultWindowSize, OutgoingWindow = defaultWindowSize, HandleMax = DefaultMaxLinks - 1 })
        {
        }

        internal Session(Connection connection, Begin begin)
        {
            this.connection = connection;
            this.handleMax = begin.HandleMax;
            this.nextOutgoingId = uint.MaxValue - 2u;
            this.incomingWindow = defaultWindowSize;
            this.outgoingWindow = begin.IncomingWindow;
            this.incomingDeliveryId = uint.MaxValue;
            this.localLinks = new Link[1];
            this.remoteLinks = new Link[1];
            this.incomingList = new LinkedList();
            this.outgoingList = new LinkedList();
            this.channel = connection.AddSession(this);

            begin.IncomingWindow = this.incomingWindow;
            begin.NextOutgoingId = this.nextOutgoingId;
            this.state = State.BeginSent;
            this.SendBegin(begin);
#if TRACE
            Trace.WriteLine(TraceLevel.Information, "session begin");
#endif
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
                int count = this.localLinks.Length;
                for (int i = 0; i < count; ++i)
                {
                    if (this.localLinks[i] == null)
                    {
                        this.localLinks[i] = link;
                        return (uint)i;
                    }
                }

                if (count - 1 < this.handleMax)
                {
                    int size = Math.Min(count * 2, (int)this.handleMax + 1);
                    Link[] expanded = new Link[size];
                    Array.Copy(this.localLinks, expanded, count);
                    this.localLinks = expanded;
                    this.localLinks[count] = link;
                    return (ushort)count;
                }

#if !TRACE
                throw new AmqpException(ErrorCode.AmqpHandleExceeded);
#else
                throw new AmqpException(ErrorCode.NotAllowed,
                    Fx.Format(SRAmqp.AmqpHandleExceeded, this.handleMax + 1));
#endif
            }
        }

        internal void SendDelivery(Delivery delivery)
        {
            lock (this.ThisLock)
            {
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
#if !TRACE
                    throw new AmqpException(ErrorCode.IllegalOperationStateOnBegin, this.state.ToString());
#else
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "OnBegin", this.state));
#endif
                }

                this.outgoingWindow = begin.IncomingWindow;
            }

            if (begin.HandleMax < this.handleMax)
            {
                this.handleMax = begin.HandleMax;
            }
        }

        internal bool OnEnd(End end)
        {
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
#if !TRACE
                    throw new AmqpException(ErrorCode.IllegalOperationStateOnEnd, this.state.ToString());
#else
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "OnEnd", this.state));
#endif
                }

                this.OnClose(end.Error);
                this.NotifyClosed(end.Error);
                return true;
            }
        }

        internal void OnCommand(DescribedList command, ByteBuffer buffer)
        {
#if TRACE
            Fx.Assert(this.state < State.EndReceived, "Session is ending or ended and cannot receive commands.");
#endif
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
#if !TRACE
                throw new NotImplementedException(command.Descriptor.Name);
#else
                throw new AmqpException(ErrorCode.NotImplemented,
                    Fx.Format(SRAmqp.AmqpOperationNotSupported, command.Descriptor.Name));
#endif
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
#if !TRACE
                    throw new AmqpException(ErrorCode.IllegalOperationStateClose, this.state.ToString());
#else
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "Close", this.state));
#endif
                }

                this.SendEnd();
                return this.state == State.End;
            }
        }

        internal virtual void OnAttach(Attach attach)
        {
            lock (this.ThisLock)
            {
                if (attach.Handle > this.handleMax)
                {
#if !TRACE
                    throw new AmqpException(ErrorCode.AmqpHandleExceeded);
#else
                    throw new AmqpException(ErrorCode.NotAllowed,
                        Fx.Format(SRAmqp.AmqpHandleExceeded, this.handleMax + 1));
#endif
                }

                for (int i = 0; i < this.localLinks.Length; ++i)
                {
                    Link link = this.localLinks[i];
                    if (link != null && string.Compare(link.Name, attach.LinkName) == 0)
                    {
                        link.OnAttach(attach.Handle, attach);

                        int count = this.remoteLinks.Length;
                        if (count - 1 < attach.Handle)
                        {
                            int size = Math.Min(count * 2, (int)this.handleMax + 1);
                            Link[] expanded = new Link[size];
                            Array.Copy(this.remoteLinks, expanded, count);
                            this.remoteLinks = expanded;
                        }

                        var remoteLink = this.remoteLinks[attach.Handle];
                        if (remoteLink != null)
                        {
#if !TRACE
                            throw new AmqpException(ErrorCode.HandleInUse, attach.Handle + " "+  remoteLink.Name);
#else
                            throw new AmqpException(ErrorCode.HandleInUse,
                                Fx.Format(SRAmqp.AmqpHandleInUse, attach.Handle, remoteLink.Name));
#endif
                        }

                        this.remoteLinks[attach.Handle] = link;
                        return;
                    }
                }
            }

#if !TRACE
            throw new AmqpException(ErrorCode.LinkNotFound, attach.LinkName);
#else
            throw new AmqpException(ErrorCode.NotFound,
                Fx.Format(SRAmqp.LinkNotFound, attach.LinkName));
#endif
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
            if (link.OnDetach(detach))
            {
                lock (this.ThisLock)
                {
                    this.localLinks[link.Handle] = null;
                    this.remoteLinks[detach.Handle] = null;
                }
            }
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
                if (this.incomingWindow-- == 0)
                {
                    this.SendFlow(new Flow());
                }

                this.nextIncomingId++;
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
                        delivery.OnStateChange(dispose.State);
                        if (delivery.Settled)
                        {
                            linkedList.Remove(delivery);
                        }
                    }

                    delivery = next;
                }
            }

        }

        void ThrowIfEnded(string operation)
        {
            if (this.state >= State.EndPipe)
            {
#if !TRACE
                throw new AmqpException(ErrorCode.IllegalOperationState, operation + " @ " + this.state.ToString());
#else
                throw new AmqpException(this.Error ??
                    new Error()
                    {
                        Condition = ErrorCode.IllegalState,
                        Description = Fx.Format(SRAmqp.AmqpIllegalOperationState, operation, this.state)
                    });
#endif
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
#if !TRACE
                    throw new AmqpException(ErrorCode.HandleNotFound, remoteHandle.ToString() + ":" + this.channel.ToString());
#else
                    throw new AmqpException(ErrorCode.NotFound,
                        Fx.Format(SRAmqp.AmqpHandleNotFound, remoteHandle, this.channel));
#endif
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
            while (this.outgoingWindow > 0 && delivery != null && delivery.Buffer.Length > 0)
            {
                --this.outgoingWindow;
                Transfer transfer = new Transfer() { Handle = delivery.Handle };

                if (delivery.BytesTransfered == 0)
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

                int len = delivery.Buffer.Length;
                this.connection.SendCommand(this.channel, transfer, delivery.Buffer);
                delivery.BytesTransfered += len - delivery.Buffer.Length;

                if (delivery.Buffer.Length == 0)
                {
                    delivery.Buffer = null;
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
