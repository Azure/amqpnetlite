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

    /// <summary>
    /// The state of a link.
    /// </summary>
    public enum LinkState
    {
        /// <summary>
        /// The link is started.
        /// </summary>
        Start,
        
        /// <summary>
        /// Attach frame was sent.
        /// </summary>
        AttachSent,
        
        /// <summary>
        /// Attach frame was received.
        /// </summary>
        AttachReceived,
        
        /// <summary>
        /// The link is attached.
        /// </summary>
        Attached,
        
        /// <summary>
        /// The link is detaching. 
        /// </summary>
        DetachPipe,
        
        /// <summary>
        /// Detach frame was sent. 
        /// </summary>
        DetachSent,
        
        /// <summary>
        /// Detach frame was received. 
        /// </summary>
        DetachReceived,
        
        /// <summary>
        /// The link is closed.
        /// </summary>
        End
    }

    /// <summary>
    /// The Link class represents an AMQP link.
    /// </summary>
    public abstract partial class Link : AmqpObject
    {
        readonly Session session;
        readonly string name;
        readonly uint handle;
        readonly OnAttached onAttached;
        LinkState state;
        bool detach;
        private readonly object lockObject = new object();

        /// <summary>
        /// Initializes the link.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="name">The link name.</param>
        /// <param name="onAttached">The callback to handle received attach.</param>
        protected Link(Session session, string name, OnAttached onAttached)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            this.session = session;
            this.name = name;
            this.onAttached = onAttached;
            this.handle = session.AddLink(this);
            this.state = LinkState.Start;
        }

        /// <summary>
        /// Gets the link name.
        /// </summary>
        public string Name
        {
            get { return this.name; }
        }

        /// <summary>
        /// Gets the link handle.
        /// </summary>
        public uint Handle
        {
            get { return this.handle; }
        }

        /// <summary>
        /// Gets the session where the link was created.
        /// </summary>
        public Session Session
        {
            get { return this.session; }
        }
        
        /// <summary>
        /// Gets the link state.
        /// </summary>
        public LinkState LinkState
        {
            get { return this.state; }
        }

        internal object ThisLock
        {
            get { return this.lockObject; }
        }

        internal bool IsDetaching
        {
            get { return this.state >= LinkState.DetachPipe; }
        }

        /// <summary>
        /// Detaches the link endpoint without closing it.
        /// </summary>
        /// <param name="error">The error causing a detach.</param>
        /// <remarks>
        /// An exception will be thrown if the peer responded with an error
        /// or the link was closed instead of being detached.
        /// </remarks>
        public void Detach(Error error = null)
        {
            this.detach = true;
            this.CloseInternal(DefaultTimeout, error);
        }

        internal void Abort(Error error, string reason)
        {
            this.CloseCalled = true;
            this.Error = error ?? new Error(ErrorCode.DetachForced) { Description = reason };

            this.OnAbort(error);

            if (this.state != LinkState.End)
            {
                this.state = LinkState.End;
                this.NotifyClosed(this.Error);
            }
        }

        internal virtual void OnAttach(uint remoteHandle, Attach attach)
        {
            IHandler handler = this.session.Connection.Handler;
            if (handler != null && handler.CanHandle(EventId.LinkRemoteOpen))
            {
                handler.Handle(Event.Create(EventId.LinkRemoteOpen, this.session.Connection, this.session, this, context: attach));
            }

            lock (this.ThisLock)
            {
                if (this.state == LinkState.AttachSent)
                {
                    this.state = LinkState.Attached;
                }
                else if (this.state == LinkState.DetachPipe)
                {
                    this.state = LinkState.DetachSent;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "OnAttach", this.state));
                }
            }

            if (this.onAttached != null)
            {
                this.onAttached(this, attach);
            }
        }

        internal bool OnDetach(Detach detach)
        {
            IHandler handler = this.session.Connection.Handler;
            if (handler != null && handler.CanHandle(EventId.LinkRemoteClose))
            {
                handler.Handle(Event.Create(EventId.LinkRemoteClose, this.session.Connection, this.session, this, context: detach));
            }

            Error remoteError = detach.Error;
            if (remoteError == null && this.detach && detach.Closed)
            {
                remoteError = new Error(ErrorCode.InternalError)
                {
                    Description = "Link is closed by peer though a detach was requested."
                };
            }

            this.Error = remoteError;

            lock (this.ThisLock)
            {
                if (this.state == LinkState.DetachSent)
                {
                    this.state = LinkState.End;
                }
                else if (this.state == LinkState.Attached)
                {
                    this.SendDetach(null);
                    this.state = LinkState.End;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "OnDetach", this.state));
                }

                this.OnClose(remoteError);
            }

            this.session.RemoveLink(this, detach.Handle);
            this.NotifyClosed(remoteError);

            return true;
        }

        internal abstract void OnFlow(Flow flow);

        internal abstract void OnTransfer(Delivery delivery, Transfer transfer, ByteBuffer buffer);

        internal abstract void OnDeliveryStateChanged(Delivery delivery);

        /// <summary>
        /// Aborts the link.
        /// </summary>
        /// <param name="error">The <see cref="Error"/> for aborting the link.</param>
        protected abstract void OnAbort(Error error);

        /// <summary>
        /// Closes the link.
        /// </summary>
        /// <param name="error">The <see cref="Error"/> for closing the link.</param>
        /// <returns></returns>
        protected override bool OnClose(Error error)
        {
            lock (this.ThisLock)
            {
                if (this.state == LinkState.End)
                {
                    return true;
                }
                else if (this.state == LinkState.AttachSent)
                {
                    this.state = LinkState.DetachPipe;
                }
                else if (this.state == LinkState.Attached)
                {
                    this.state = LinkState.DetachSent;
                }
                else if (this.state == LinkState.DetachReceived)
                {
                    this.state = LinkState.End;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "Close", this.state));
                }

                this.SendDetach(error);
                return this.state == LinkState.End;
            }
        }

        internal void SendFlow(uint deliveryCount, uint credit, bool drain)
        {
            lock (this.ThisLock)
            {
                if (!this.IsDetaching)
                {
                    Flow flow = new Flow() { Handle = this.handle, DeliveryCount = deliveryCount, LinkCredit = credit, Drain = drain };
                    this.session.SendFlow(flow);
                }
            }
        }

        internal void SendAttach(bool role, uint initialDeliveryCount, Attach attach)
        {
            Fx.Assert(this.state == LinkState.Start, "state must be Start");
            this.state = LinkState.AttachSent;
            attach.LinkName = this.name;
            attach.Handle = this.handle;
            attach.Role = role;
            if (!role)
            {
                attach.InitialDeliveryCount = initialDeliveryCount;
            }

            IHandler handler = this.session.Connection.Handler;
            if (handler != null && handler.CanHandle(EventId.LinkLocalOpen))
            {
                handler.Handle(Event.Create(EventId.LinkLocalOpen, this.session.Connection, this.session, this, context: attach));
            }

            this.session.SendCommand(attach);
        }

        internal void ThrowIfDetaching(string operation)
        {
            if (this.IsDetaching || this.CloseCalled)
            {
                throw new AmqpException(this.Error ??
                    new Error(ErrorCode.IllegalState)
                    {
                        Description = Fx.Format(SRAmqp.AmqpIllegalOperationState, operation, this.state)
                    });
            }
        }

        void SendDetach(Error error)
        {
            Detach detach = new Detach() { Handle = this.handle, Error = error, Closed = !this.detach };
            IHandler handler = this.session.Connection.Handler;
            if (handler != null && handler.CanHandle(EventId.LinkLocalClose))
            {
                handler.Handle(Event.Create(EventId.LinkLocalClose, this.session.Connection, this.session, this, context: detach));
            }

            this.session.SendCommand(detach);
        }
    }
}