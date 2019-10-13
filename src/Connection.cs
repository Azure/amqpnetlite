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
    using System.Threading;
    using Amqp.Framing;
    using Amqp.Handler;
    using Amqp.Sasl;
    using Amqp.Types;
    
    /// <summary>
    /// The state of a connection.
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        /// The connection is started.
        /// </summary>
        Start,
        
        /// <summary>
        /// Header frame was sent. 
        /// </summary>
        HeaderSent,
        
        /// <summary>
        /// The connection is opening.
        /// </summary>
        OpenPipe,
        
        /// <summary>
        /// Header frame was received.
        /// </summary>
        HeaderReceived,
        
        /// <summary>
        /// Header frame exchanged.
        /// </summary>
        HeaderExchanged,
        
        /// <summary>
        /// Open frame was sent.
        /// </summary>
        OpenSent,
        
        /// <summary>
        /// Open frame was received.
        /// </summary>
        OpenReceived,
        
        /// <summary>
        /// The connection is opened.
        /// </summary>
        Opened,
        
        /// <summary>
        /// Close frame received.
        /// </summary>
        CloseReceived,
        
        /// <summary>
        /// Close frame sent.
        /// </summary>
        CloseSent,
        
        /// <summary>
        /// The connection is opening or closing. 
        /// </summary>
        OpenClosePipe,
        
        /// <summary>
        /// The connection is closing.
        /// </summary>
        ClosePipe,
        
        /// <summary>
        /// The connection is closed.
        /// </summary>
        End
    }

    /// <summary>
    /// The Connection class represents an AMQP connection.
    /// </summary>
    public partial class Connection : AmqpObject
    {
        /// <summary>
        /// A flag to disable server certificate validation when TLS is used.
        /// </summary>
        public static bool DisableServerCertValidation;

        internal const uint DefaultMaxFrameSize = 256 * 1024;
        internal const ushort DefaultMaxSessions = 256;
        internal const int DefaultMaxLinksPerSession = 64;
        const uint MaxIdleTimeout = 30 * 60 * 1000;
        readonly Address address;
        readonly OnOpened onOpened;
        IHandler handler;
        Session[] localSessions;
        Session[] remoteSessions;
        ushort channelMax;
        ConnectionState state;
        uint maxFrameSize;
        uint remoteMaxFrameSize;
        ITransport writer;
        Pump reader;
        HeartBeat heartBeat;
        private readonly object lockObject = new object();

        Connection(Address address, ushort channelMax, uint maxFrameSize)
        {
            this.address = address;
            this.channelMax = channelMax;
            this.maxFrameSize = maxFrameSize;
            this.remoteMaxFrameSize = uint.MaxValue;
            this.localSessions = new Session[1];
            this.remoteSessions = new Session[1];
        }

        /// <summary>
        /// Initializes a connection from the address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <remarks>
        /// The connection initialization includes establishing the underlying transport,
        /// which typically has blocking network I/O. Depending on the current synchronization
        /// context, it may cause deadlock or UI freeze. Please use the ConnectionFactory.CreateAsync
        /// method instead.
        /// </remarks>
        public Connection(Address address)
            : this(address, null)
        {
        }

        /// <summary>
        /// Initializes a connection from the address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="handler">The protocol handler.</param>
        public Connection(Address address, IHandler handler)
            : this(address, DefaultMaxSessions, DefaultMaxFrameSize)
        {
            this.handler = handler;
            this.Connect(null, null);
        }

        /// <summary>
        /// Initializes a connection with SASL profile, open and open callback.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="saslProfile">The SASL profile to do authentication (optional). If it is
        /// null and address has user info, SASL PLAIN profile is used.</param>
        /// <param name="open">The open frame to send (optional). If not null, all mandatory
        /// fields must be set. Ensure that other fields are set to desired values.</param>
        /// <param name="onOpened">The callback to handle remote open frame (optional).</param>
        /// <remarks>
        /// The connection initialization includes establishing the underlying transport,
        /// which typically has blocking network I/O. Depending on the current synchronization
        /// context, it may cause deadlock or UI freeze. Please use the ConnectionFactory.CreateAsync
        /// method instead.
        /// </remarks>
        public Connection(Address address, SaslProfile saslProfile, Open open, OnOpened onOpened)
            : this(address, DefaultMaxSessions, DefaultMaxFrameSize)
        {
            this.onOpened = onOpened;
            this.Connect(saslProfile, open);
        }

        /// <summary>
        /// Gets the protocol handler on the connection if it is set.
        /// </summary>
        public IHandler Handler
        {
            get { return this.handler; }
        }

        object ThisLock
        {
            get { return this.lockObject; }
        }

#if NETFX || NETFX40 || DOTNET || NETFX_CORE || WINDOWS_STORE || WINDOWS_PHONE
        internal Connection(IBufferManager bufferManager, AmqpSettings amqpSettings, Address address,
            IAsyncTransport transport, Open open, OnOpened onOpened, IHandler handler)
            : this(address, (ushort)(amqpSettings.MaxSessionsPerConnection - 1), (uint)amqpSettings.MaxFrameSize)
        {
            transport.SetConnection(this);

            this.handler = handler;
            this.BufferManager = bufferManager;
            this.MaxLinksPerSession = amqpSettings.MaxLinksPerSession;
            this.onOpened = onOpened;
            this.writer = new TransportWriter(transport, this.OnIoException);

            // after getting the transport, move state to open pipe before starting the pump
            if (open == null)
            {
                open = new Open()
                {
                    ContainerId = amqpSettings.ContainerId,
                    HostName = amqpSettings.HostName ?? this.address.Host,
                    ChannelMax = this.channelMax,
                    MaxFrameSize = this.maxFrameSize,
                    IdleTimeOut = (uint)amqpSettings.IdleTimeout
                };
            }

            if (open.IdleTimeOut > 0)
            {
                this.heartBeat = new HeartBeat(this, open.IdleTimeOut);
            }

            this.SendHeader();
            this.SendOpen(open);
            this.state = ConnectionState.OpenPipe;
        }

        /// <summary>
        /// Gets a factory with default settings.
        /// </summary>
        public static ConnectionFactory Factory
        {
            get { return new ConnectionFactory(); }
        }

        /// <summary>
        /// Gets the connection state.
        /// </summary>
        public ConnectionState ConnectionState
        {
            get { return this.state; }
        }

        internal IBufferManager BufferManager
        {
            get;
            private set;
        }

        internal uint MaxFrameSize
        {
            get { return this.maxFrameSize; }
        }

        internal int MaxLinksPerSession;

        ByteBuffer AllocateBuffer(int size)
        {
            return this.BufferManager.GetByteBuffer(size);
        }

        ByteBuffer WrapBuffer(ByteBuffer buffer, int offset, int length)
        {
            return new WrappedByteBuffer(buffer, offset, length);
        }
#else
        internal int MaxLinksPerSession = DefaultMaxLinksPerSession;

        ByteBuffer AllocateBuffer(int size)
        {
            return new ByteBuffer(size, true);
        }

        ByteBuffer WrapBuffer(ByteBuffer buffer, int offset, int length)
        {
            return new ByteBuffer(buffer.Buffer, offset, length, length);
        }
#endif

        internal ushort AddSession(Session session)
        {
            this.ThrowIfClosed("AddSession");
            lock (this.ThisLock)
            {
                int count = this.localSessions.Length;
                for (int i = 0; i < count; ++i)
                {
                    if (this.localSessions[i] == null)
                    {
                        this.localSessions[i] = session;
                        return (ushort)i;
                    }
                }

                if (count - 1 < this.channelMax)
                {
                    int size = Math.Min(count * 2, this.channelMax + 1);
                    Session[] expanded = new Session[size];
                    Array.Copy(this.localSessions, expanded, count);
                    this.localSessions = expanded;
                    this.localSessions[count] = session;
                    return (ushort)count;
                }

                throw new AmqpException(ErrorCode.NotAllowed,
                    Fx.Format(SRAmqp.AmqpHandleExceeded, this.channelMax));
            }
        }

        internal void SendCommand(ushort channel, DescribedList command)
        {
            if (command.Descriptor.Code == Codec.Close.Code || this.state < ConnectionState.CloseSent)
            {
                ByteBuffer buffer = this.AllocateBuffer(Frame.CmdBufferSize);
                Frame.Encode(buffer, FrameType.Amqp, channel, command);
                this.writer.Send(buffer);
                if (this.heartBeat != null)
                {
                    this.heartBeat.OnSend();
                }

                if (Trace.TraceLevel >= TraceLevel.Frame)
                {
                    Trace.WriteLine(TraceLevel.Frame, "SEND (ch={0}) {1}", channel, command);
                }
            }
        }

        internal int SendCommand(ushort channel, Transfer transfer, bool first, ByteBuffer payload, int reservedBytes)
        {
            this.ThrowIfClosed("Send");
            ByteBuffer buffer = this.AllocateBuffer(Frame.CmdBufferSize);
            Frame.Encode(buffer, FrameType.Amqp, channel, transfer);
            int payloadSize = payload.Length;
            int frameSize = buffer.Length + payloadSize;
            bool more = frameSize > this.remoteMaxFrameSize;
            if (more)
            {
                transfer.More = true;
                buffer.Reset();
                Frame.Encode(buffer, FrameType.Amqp, channel, transfer);
                frameSize = (int)this.remoteMaxFrameSize;
                payloadSize = frameSize - buffer.Length;
            }

            AmqpBitConverter.WriteInt(buffer.Buffer, buffer.Offset, frameSize);

            ByteBuffer frameBuffer;
            if (first && !more && reservedBytes >= buffer.Length)
            {
                // optimize for most common case: single-transfer message
                frameBuffer = this.WrapBuffer(payload, payload.Offset - buffer.Length, frameSize);
                Array.Copy(buffer.Buffer, buffer.Offset, frameBuffer.Buffer, frameBuffer.Offset, buffer.Length);
                buffer.ReleaseReference();
            }
            else
            {
                AmqpBitConverter.WriteBytes(buffer, payload.Buffer, payload.Offset, payloadSize);
                frameBuffer = buffer;
            }

            payload.Complete(payloadSize);
            this.writer.Send(frameBuffer);
            if (Trace.TraceLevel >= TraceLevel.Frame)
            {
                Trace.WriteLine(TraceLevel.Frame, "SEND (ch={0}) {1} payload {2}", channel, transfer, payloadSize);
            }

            return payloadSize;
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        protected override bool OnClose(Error error)
        {
            lock (this.ThisLock)
            {
                ConnectionState newState = ConnectionState.Start;
                if (this.state == ConnectionState.OpenPipe )
                {
                    newState = ConnectionState.OpenClosePipe;
                }
                else if (state == ConnectionState.OpenSent)
                {
                    newState = ConnectionState.ClosePipe;
                }
                else if (this.state == ConnectionState.Opened)
                {
                    newState = ConnectionState.CloseSent;
                }
                else if (this.state == ConnectionState.CloseReceived)
                {
                    newState = ConnectionState.End;
                }
                else if (this.state == ConnectionState.End)
                {
                    return true;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "Close", this.state));
                }

                this.SendClose(error);
                this.state = newState;
                return this.state == ConnectionState.End;
            }
        }

        void Connect(SaslProfile saslProfile, Open open)
        {
            if (open != null)
            {
                this.maxFrameSize = open.MaxFrameSize;
                this.channelMax = open.ChannelMax;
            }
            else
            {
                open = new Open()
                {
                    ContainerId = Guid.NewGuid().ToString(),
                    HostName = this.address.Host,
                    MaxFrameSize = this.maxFrameSize,
                    ChannelMax = this.channelMax
                };
            }

            if (open.IdleTimeOut > 0)
            {
                this.heartBeat = new HeartBeat(this, open.IdleTimeOut);
            }

            ITransport transport;
#if NETFX
            if (WebSocketTransport.MatchScheme(address.Scheme))
            {
                WebSocketTransport wsTransport = new WebSocketTransport();
                wsTransport.ConnectAsync(address, null).ConfigureAwait(false).GetAwaiter().GetResult();
                transport = wsTransport;
            }
            else
#endif
            {
                TcpTransport tcpTransport = new TcpTransport();
                tcpTransport.Connect(this, this.address, DisableServerCertValidation);
                transport = tcpTransport;
            }

            try
            {
                if (saslProfile != null)
                {
                    transport = saslProfile.Open(this.address.Host, transport);
                }
                else if (this.address.User != null)
                {
                    transport = new SaslPlainProfile(this.address.User, this.address.Password).Open(this.address.Host, transport);
                }
            }
            catch
            {
                transport.Close();
                throw;
            }

            this.writer = new Writer(transport);

            // after getting the transport, move state to open pipe before starting the pump
            this.SendHeader();
            this.SendOpen(open);
            this.state = ConnectionState.OpenPipe;

            this.reader = new Pump(this, transport);
            this.reader.Start();
        }

        void ThrowIfClosed(string operation)
        {
            if (this.state >= ConnectionState.CloseSent)
            {
                throw new AmqpException(this.Error ??
                    new Error(ErrorCode.IllegalState)
                    {
                        Description = Fx.Format(SRAmqp.AmqpIllegalOperationState, operation, this.state)
                    });
            }
        }

        void SendHeader()
        {
            byte[] header = new byte[] { (byte)'A', (byte)'M', (byte)'Q', (byte)'P', 0, 1, 0, 0 };
            this.writer.Send(new ByteBuffer(header, 0, header.Length, header.Length));
            Trace.WriteLine(TraceLevel.Frame, "SEND AMQP 0 1.0.0");
        }

        void SendOpen(Open open)
        {
            IHandler handler = this.Handler;
            if (handler != null && handler.CanHandle(EventId.ConnectionLocalOpen))
            {
                handler.Handle(Event.Create(EventId.ConnectionLocalOpen, this, context: open));
            }

            this.SendCommand(0, open);
        }

        void SendClose(Error error)
        {
            Close close = new Close() { Error = error };
            IHandler handler = this.Handler;
            if (handler != null && handler.CanHandle(EventId.ConnectionLocalClose))
            {
                handler.Handle(Event.Create(EventId.ConnectionLocalClose, this, context: close));
            }

            this.SendCommand(0, close);
        }

        void OnOpen(Open open)
        {
            IHandler handler = this.Handler;
            if (handler != null && handler.CanHandle(EventId.ConnectionRemoteOpen))
            {
                handler.Handle(Event.Create(EventId.ConnectionRemoteOpen, this, context: open));
            }

            lock (this.ThisLock)
            {
                if (this.state == ConnectionState.OpenSent)
                {
                    this.state = ConnectionState.Opened;
                }
                else if (this.state == ConnectionState.ClosePipe)
                {
                    this.state = ConnectionState.CloseSent;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "OnOpen", this.state));
                }
            }

            if (this.onOpened != null)
            {
                this.onOpened(this, open);
            }

            if (open.ChannelMax < this.channelMax)
            {
                this.channelMax = open.ChannelMax;
            }

            this.remoteMaxFrameSize = open.MaxFrameSize;
            uint idleTimeout = open.IdleTimeOut;
            if (idleTimeout > 0 && this.heartBeat == null)
            {
                this.heartBeat = new HeartBeat(this, 0);
            }

            if (this.heartBeat != null)
            {
                this.heartBeat.Start(idleTimeout);
            }
        }

        void OnClose(Close close)
        {
            IHandler handler = this.Handler;
            if (handler != null && handler.CanHandle(EventId.ConnectionRemoteClose))
            {
                handler.Handle(Event.Create(EventId.ConnectionRemoteClose, this, context: close));
            }

            lock (this.ThisLock)
            {
                if (this.state == ConnectionState.Opened)
                {
                    this.SendClose(null);
                }
                else if (this.state == ConnectionState.CloseSent)
                {
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "OnClose", this.state));
                }

                this.state = ConnectionState.End;
                this.OnEnded(close.Error);
            }
        }

        internal virtual void OnBegin(ushort remoteChannel, Begin begin)
        {
            this.ValidateChannel(remoteChannel);

            lock (this.ThisLock)
            {
                Session session = this.GetSession(this.localSessions, begin.RemoteChannel);
                session.OnBegin(remoteChannel, begin);

                int count = this.remoteSessions.Length;
                if (count - 1 < remoteChannel)
                {
                    int size = count * 2;
                    while (size - 1 < remoteChannel)
                    {
                        size *= 2;
                    }

                    Session[] expanded = new Session[size];
                    Array.Copy(this.remoteSessions, expanded, count);
                    this.remoteSessions = expanded;
                }

                var remoteSession = this.remoteSessions[remoteChannel];
                if (remoteSession != null)
                {
                    throw new AmqpException(ErrorCode.HandleInUse,
                        Fx.Format(SRAmqp.AmqpHandleInUse, remoteChannel, remoteSession.GetType().Name));
                }

                this.remoteSessions[remoteChannel] = session;
            }
        }

        internal void ValidateChannel(ushort channel)
        {
            if (channel > this.channelMax)
            {
                throw new AmqpException(ErrorCode.NotAllowed,
                    Fx.Format(SRAmqp.AmqpHandleExceeded, this.channelMax + 1));
            }
        }

        void OnEnd(ushort remoteChannel, End end)
        {
            Session session = this.GetSession(this.remoteSessions, remoteChannel);
            if (session.OnEnd(end))
            {
                lock (this.ThisLock)
                {
                    this.localSessions[session.Channel] = null;
                    this.remoteSessions[remoteChannel] = null;
                }
            }
        }

        void OnSessionCommand(ushort remoteChannel, DescribedList command, ByteBuffer buffer)
        {
            this.GetSession(this.remoteSessions, remoteChannel).OnCommand(command, buffer);
        }

        Session GetSession(Session[] sessions, ushort channel)
        {
            lock (this.ThisLock)
            {
                Session session = null;
                if (channel < sessions.Length)
                {
                    session = sessions[channel];
                }

                if (session == null)
                {
                    throw new AmqpException(ErrorCode.NotFound,
                        Fx.Format(SRAmqp.AmqpChannelNotFound, channel));
                }

                return session;
            }
        }

        internal bool OnHeader(ProtocolHeader header)
        {
            Trace.WriteLine(TraceLevel.Frame, "RECV AMQP {0}", header);
            if (header.Id != 0 || header.Major != 1 || header.Minor != 0 || header.Revision != 0)
            {
                throw new AmqpException(ErrorCode.NotImplemented,
                    Fx.Format(SRAmqp.AmqpProtocolMismatch, header, "0 1 0 0"));
            }

            lock (this.ThisLock)
            {
                if (this.state == ConnectionState.OpenPipe)
                {
                    this.state = ConnectionState.OpenSent;
                }
                else if (this.state == ConnectionState.OpenClosePipe)
                {
                    this.state = ConnectionState.ClosePipe;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "OnHeader", this.state));
                }
            }

            return true;
        }

        internal bool OnFrame(ByteBuffer buffer)
        {
            bool shouldContinue = true;
            try
            {
                ushort channel;
                DescribedList command;
                Frame.Decode(buffer, out channel, out command);
                if (Trace.TraceLevel >= TraceLevel.Frame)
                {
                    if (buffer.Length > 0)
                    {
                        Trace.WriteLine(TraceLevel.Frame, "RECV (ch={0}) {1} payload {2}", channel, command, buffer.Length);
                    }
                    else
                    {
                        Trace.WriteLine(TraceLevel.Frame, "RECV (ch={0}) {1}", channel, command);
                    }
                }

                if (this.heartBeat != null)
                {
                    this.heartBeat.OnReceive();
                }

                if (command != null)
                {
                    if (command.Descriptor.Code == Codec.Open.Code)
                    {
                        this.OnOpen((Open)command);
                    }
                    else if (command.Descriptor.Code == Codec.Close.Code)
                    {
                        this.OnClose((Close)command);
                        shouldContinue = false;
                    }
                    else if (command.Descriptor.Code == Codec.Begin.Code)
                    {
                        this.OnBegin(channel, (Begin)command);
                    }
                    else if (command.Descriptor.Code == Codec.End.Code)
                    {
                        this.OnEnd(channel, (End)command);
                    }
                    else
                    {
                        this.OnSessionCommand(channel, command, buffer);
                    }
                }
            }
            catch (Exception exception)
            {
                this.OnException(exception);
                shouldContinue = false;
            }

            return shouldContinue;
        }

        void OnException(Exception exception)
        {
            Trace.WriteLine(TraceLevel.Error, "Exception occurred: {0}", exception.ToString());
            AmqpException amqpException = exception as AmqpException;
            Error error = amqpException != null ?
                amqpException.Error :
                new Error(ErrorCode.InternalError) { Description = exception.Message };

            if (this.state < ConnectionState.CloseSent)
            {
                // send close and shutdown the transport.
                try
                {
                    this.CloseInternal(0, error);
                }
                catch
                {
                }
            }

            this.state = ConnectionState.End;
            this.OnEnded(error);
        }

        internal void OnIoException(Exception exception)
        {
            Trace.WriteLine(TraceLevel.Error, "I/O: {0}", exception.ToString());
            if (this.state != ConnectionState.End)
            {
                this.state = ConnectionState.End;
                this.CloseCalled = true;
                Error error = new Error(ErrorCode.ConnectionForced) { Description = exception.Message };
                this.OnEnded(error);
            }
        }

        void OnEnded(Error error)
        {
            this.Error = error;

            if (this.heartBeat != null)
            {
                this.heartBeat.Stop();
            }

            if (this.writer != null)
            {
                this.writer.Close();
            }

            for (int i = 0; i < this.localSessions.Length; i++)
            {
                var session = this.localSessions[i];
                if (session != null)
                {
                    session.Abort(this.Error);
                }
            }

            this.NotifyClosed(this.Error);
        }

        sealed class HeartBeat
        {
            readonly Connection connection;
            readonly Timer timer;
            uint local;  // for enforcing heartbeats
            uint remote; // for sending heartbeats
            DateTime lastSend;
            DateTime lastReceive;

            public HeartBeat(Connection connection, uint local)
            {
                this.connection = connection;
                this.local = local;
                this.timer = new Timer(OnTimer, this, -1, -1);
            }

            public void Start(uint remote)
            {
                this.remote = remote / 2;
                this.lastSend = DateTime.UtcNow;
                this.lastReceive = DateTime.UtcNow;
                this.SetTimer();
            }

            public void Stop()
            {
                if (this.timer != null)
                {
                    this.timer.Dispose();
                }
            }

            public void OnSend()
            {
                this.lastSend = DateTime.UtcNow;
            }

            public void OnReceive()
            {
                this.lastReceive = DateTime.UtcNow;
            }

            static void OnTimer(object state)
            {
                var thisPtr = (HeartBeat)state;
                try
                {
                    DateTime now = DateTime.UtcNow;
                    if (thisPtr.local > 0 &&
                        GetDueMilliseconds(thisPtr.local, now, thisPtr.lastReceive) == 0)
                    {
                        thisPtr.connection.CloseInternal(
                            0,
                            new Error(ErrorCode.ConnectionForced)
                            {
                                Description = Fx.Format("Connection closed after idle timeout {0} ms", thisPtr.local)
                            });
                        return;
                    }

                    if (thisPtr.remote > 0 &&
                        GetDueMilliseconds(thisPtr.remote, now, thisPtr.lastSend) == 0)
                    {
                        thisPtr.connection.writer.Send(new ByteBuffer(new byte[] { 0, 0, 0, 8, 2, 0, 0, 0 }, 0, 8, 8));
                        thisPtr.OnSend();
                        Trace.WriteLine(TraceLevel.Frame, "SEND (ch=0) empty");
                    }
                }
                catch (Exception exception)
                {
                    Trace.WriteLine(TraceLevel.Warning, "{0}:{1}", exception.GetType().Name, exception.Message);
                }
                finally
                {
                    if (!thisPtr.connection.IsClosed)
                    {
                        thisPtr.SetTimer();
                    }
                }
            }

            static uint GetDueMilliseconds(uint timeout, DateTime now, DateTime last)
            {
                uint due = uint.MaxValue;
                if (timeout > 0)
                {
                    uint elapsed = (uint)((now.Ticks - last.Ticks) / Encoder.TicksPerMillisecond);
                    due = timeout > elapsed ? timeout - elapsed : 0;
                }

                return due;
            }

            void SetTimer()
            {
                DateTime now = DateTime.UtcNow;
                uint localDue = GetDueMilliseconds(this.local, now, this.lastReceive);
                uint remoteDue = GetDueMilliseconds(this.remote, now, this.lastSend);
                uint due = localDue < remoteDue ? localDue : remoteDue;
                Fx.Assert(due < uint.MaxValue, "At least one timeout should be set");
                this.timer.Change(due > int.MaxValue ? int.MaxValue : (int)due, -1);
            }
        }

        // Writer and Pump are for synchronous transport created from the constructors

#if NETMF
        sealed class Writer : System.Collections.Queue, ITransport
#else
        sealed class Writer : System.Collections.Generic.Queue<ByteBuffer>, ITransport
#endif
        {
            readonly ITransport transport;

            public Writer(ITransport transport)
            {
                this.transport = transport;
            }

            void ITransport.Send(ByteBuffer buffer)
            {
                lock (this)
                {
                    this.Enqueue(buffer);
                    if (this.Count > 1)
                    {
                        return;
                    }
                }

                while (buffer != null)
                {
                    this.transport.Send(buffer);
                    lock (this)
                    {
                        this.Dequeue();
                        if (this.Count > 0)
                        {
#if NETMF
                            buffer = (ByteBuffer)this.Peek();
#else
                            buffer = this.Peek();
#endif
                        }
                        else
                        {
                            buffer = null;
                        }
                    }
                }
            }

            int ITransport.Receive(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            void ITransport.Close()
            {
                this.transport.Close();
            }
        }

        sealed class Pump
        {
            readonly Connection connection;
            readonly ITransport transport;

            public Pump(Connection connection, ITransport transport)
            {
                this.connection = connection;
                this.transport = transport;
            }

            public void Start()
            {
                Fx.StartThread(this.PumpThread);
            }

            void PumpThread()
            {
                try
                {
                    ProtocolHeader header = Reader.ReadHeader(this.transport);
                    this.connection.OnHeader(header);
                }
                catch (Exception exception)
                {
                    this.connection.OnIoException(exception);
                    return;
                }

                byte[] sizeBuffer = new byte[FixedWidth.UInt];
                while (this.connection.state != ConnectionState.End)
                {
                    try
                    {
                        ByteBuffer buffer = Reader.ReadFrameBuffer(this.transport, sizeBuffer, this.connection.maxFrameSize);
                        this.connection.OnFrame(buffer);
                    }
                    catch (Exception exception)
                    {
                        this.connection.OnIoException(exception);
                    }
                }
            }
        }
    }
}
