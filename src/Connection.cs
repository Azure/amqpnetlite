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
    using Amqp.Sasl;
    using Amqp.Types;

    /// <summary>
    /// The callback that is invoked when an open frame is received from peer.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="open">The received open frame.</param>
    public delegate void OnOpened(Connection connection, Open open);

    /// <summary>
    /// The Connection class represents an AMQP connection.
    /// </summary>
    public class Connection : AmqpObject
    {
        enum State
        {
            Start,
            HeaderSent,
            OpenPipe,
            OpenClosePipe,
            HeaderReceived,
            HeaderExchanged,
            OpenSent,
            OpenReceived,
            Opened,
            CloseReceived,
            ClosePipe,
            CloseSent,
            End
        }

        /// <summary>
        /// A flag to disable server certificate validation when TLS is used.
        /// </summary>
        public static bool DisableServerCertValidation;

        internal const uint DefaultMaxFrameSize = 256 * 1024;
        internal const ushort DefaultMaxSessions = 256;
        internal const int DefaultMaxLinksPerSession = 64;
        const uint MaxIdleTimeout = 30 * 60 * 1000;
        static readonly TimerCallback onHeartBeatTimer = OnHeartBeatTimer;
        readonly Address address;
        readonly OnOpened onOpened;
        Session[] localSessions;
        Session[] remoteSessions;
        ushort channelMax;
        State state;
        ITransport transport;
        uint maxFrameSize;
        Pump reader;
        Timer heartBeatTimer;

        Connection(ushort channelMax, uint maxFrameSize)
        {
            this.channelMax = channelMax;
            this.maxFrameSize = maxFrameSize;
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
            : this(address, null, null, null)
        {
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
            : this(DefaultMaxSessions, DefaultMaxFrameSize)
        {
            this.address = address;
            this.onOpened = onOpened;
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

            this.Connect(saslProfile, open);
        }

        object ThisLock
        {
            get { return this; }
        }

#if NETFX || NETFX40 || DOTNET || NETFX_CORE || WINDOWS_STORE || WINDOWS_PHONE
        internal Connection(IBufferManager bufferManager, AmqpSettings amqpSettings, Address address,
            IAsyncTransport transport, Open open, OnOpened onOpened)
            : this((ushort)(amqpSettings.MaxSessionsPerConnection - 1), (uint)amqpSettings.MaxFrameSize)
        {
            transport.SetConnection(this);

            this.BufferManager = bufferManager;
            this.MaxLinksPerSession = amqpSettings.MaxLinksPerSession;
            this.address = address;
            this.onOpened = onOpened;
            this.transport = new TransportWriter(transport, this.OnIoException);

            // after getting the transport, move state to open pipe before starting the pump
            if (open == null)
            {
                open = new Open()
                {
                    ContainerId = amqpSettings.ContainerId,
                    HostName = amqpSettings.HostName ?? this.address.Host,
                    ChannelMax = this.channelMax,
                    MaxFrameSize = this.maxFrameSize
                };
            }

            this.SendHeader();
            this.SendOpen(open);
            this.state = State.OpenPipe;
        }

        /// <summary>
        /// Gets a factory with default settings.
        /// </summary>
        public static ConnectionFactory Factory
        {
            get { return new ConnectionFactory(); }
        }

        internal IBufferManager BufferManager
        {
            get;
            private set;
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
            this.ThrowIfClosed("Send");
            ByteBuffer buffer = this.AllocateBuffer(Frame.CmdBufferSize);
            Frame.Encode(buffer, FrameType.Amqp, channel, command);
            this.transport.Send(buffer);
            Trace.WriteLine(TraceLevel.Frame, "SEND (ch={0}) {1}", channel, command);
        }

        internal int SendCommand(ushort channel, Transfer transfer, bool first, ByteBuffer payload, int reservedBytes)
        {
            this.ThrowIfClosed("Send");
            ByteBuffer buffer = this.AllocateBuffer(Frame.CmdBufferSize);
            Frame.Encode(buffer, FrameType.Amqp, channel, transfer);
            int payloadSize = payload.Length;
            int frameSize = buffer.Length + payloadSize;
            bool more = frameSize > this.maxFrameSize;
            if (more)
            {
                transfer.More = true;
                buffer.Reset();
                Frame.Encode(buffer, FrameType.Amqp, channel, transfer);
                frameSize = (int)this.maxFrameSize;
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
            this.transport.Send(frameBuffer);
            Trace.WriteLine(TraceLevel.Frame, "SEND (ch={0}) {1} payload {2}", channel, transfer, payloadSize);

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
                State newState = State.Start;
                if (this.state == State.OpenPipe )
                {
                    newState = State.OpenClosePipe;
                }
                else if (state == State.OpenSent)
                {
                    newState = State.ClosePipe;
                }
                else if (this.state == State.Opened)
                {
                    newState = State.CloseSent;
                }
                else if (this.state == State.CloseReceived)
                {
                    newState = State.End;
                }
                else if (this.state == State.End)
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
                return this.state == State.End;
            }
        }

        static void OnHeartBeatTimer(object state)
        {
            var thisPtr = (Connection)state;
            try
            {
                byte[] frame = new byte[] { 0, 0, 0, 8, 2, 0, 0, 0 };
                thisPtr.transport.Send(new ByteBuffer(frame, 0, frame.Length, frame.Length));
                Trace.WriteLine(TraceLevel.Frame, "SEND (ch=0) empty");
            }
            catch
            {
                // ignore failures
            }
        }

        void Connect(SaslProfile saslProfile, Open open)
        {
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

            if (saslProfile != null)
            {
                transport = saslProfile.Open(this.address.Host, transport);
            }
            else if (this.address.User != null)
            {
                transport = new SaslPlainProfile(this.address.User, this.address.Password).Open(this.address.Host, transport);
            }

            this.transport = transport;

            // after getting the transport, move state to open pipe before starting the pump
            this.SendHeader();
            this.SendOpen(open);
            this.state = State.OpenPipe;

            this.reader = new Pump(this);
            this.reader.Start();
        }

        void ThrowIfClosed(string operation)
        {
            if (this.state >= State.ClosePipe)
            {
                throw new AmqpException(this.Error ??
                    new Error()
                    {
                        Condition = ErrorCode.IllegalState,
                        Description = Fx.Format(SRAmqp.AmqpIllegalOperationState, operation, this.state)
                    });
            }
        }

        void SendHeader()
        {
            byte[] header = new byte[] { (byte)'A', (byte)'M', (byte)'Q', (byte)'P', 0, 1, 0, 0 };
            this.transport.Send(new ByteBuffer(header, 0, header.Length, header.Length));
            Trace.WriteLine(TraceLevel.Frame, "SEND AMQP 0 1.0.0");
        }

        void SendOpen(Open open)
        {
            this.SendCommand(0, open);
        }

        void SendClose(Error error)
        {
            this.SendCommand(0, new Close() { Error = error });
        }

        void OnOpen(Open open)
        {
            lock (this.ThisLock)
            {
                if (this.state == State.OpenSent)
                {
                    this.state = State.Opened;
                }
                else if (this.state == State.ClosePipe)
                {
                    this.state = State.CloseSent;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "OnOpen", this.state));
                }
            }

            if (open.ChannelMax < this.channelMax)
            {
                this.channelMax = open.ChannelMax;
            }

            if (open.MaxFrameSize < this.maxFrameSize)
            {
                this.maxFrameSize = open.MaxFrameSize;
            }

            uint idleTimeout = open.IdleTimeOut;
            if (idleTimeout > 0 && idleTimeout < uint.MaxValue)
            {
                idleTimeout -= 3000;
                if (idleTimeout > MaxIdleTimeout)
                {
                    idleTimeout = MaxIdleTimeout;
                }

                this.heartBeatTimer = new Timer(onHeartBeatTimer, this, (int)idleTimeout, (int)idleTimeout);
            }

            if (this.onOpened != null)
            {
                this.onOpened(this, open);
            }
        }

        void OnClose(Close close)
        {
            lock (this.ThisLock)
            {
                if (this.state == State.Opened)
                {
                    this.SendClose(null);
                }
                else if (this.state == State.CloseSent)
                {
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "OnClose", this.state));
                }

                this.state = State.End;
                this.OnEnded(close.Error);
            }
        }

        internal virtual void OnBegin(ushort remoteChannel, Begin begin)
        {
            lock (this.ThisLock)
            {
                if (remoteChannel > this.channelMax)
                {
                    throw new AmqpException(ErrorCode.NotAllowed,
                        Fx.Format(SRAmqp.AmqpHandleExceeded, this.channelMax + 1));
                }

                Session session = this.GetSession(this.localSessions, begin.RemoteChannel);
                session.OnBegin(remoteChannel, begin);

                int count = this.remoteSessions.Length;
                if (count - 1 < remoteChannel)
                {
                    int size = Math.Min(count * 2, this.channelMax + 1);
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
            lock (this.ThisLock)
            {
                if (this.state == State.OpenPipe)
                {
                    this.state = State.OpenSent;
                }
                else if (this.state == State.OpenClosePipe)
                {
                    this.state = State.ClosePipe;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "OnHeader", this.state));
                }

                if (header.Major != 1 || header.Minor != 0 || header.Revision != 0)
                {
                    throw new AmqpException(ErrorCode.NotImplemented, header.ToString());
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
                if (buffer.Length > 0)
                {
                    Trace.WriteLine(TraceLevel.Frame, "RECV (ch={0}) {1} payload {2}", channel, command, buffer.Length);
                }
                else
                {
                    Trace.WriteLine(TraceLevel.Frame, "RECV (ch={0}) {1}", channel, command);
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
                new Error() { Condition = ErrorCode.InternalError, Description = exception.Message };

            if (this.state < State.ClosePipe)
            {
                try
                {
                    this.Close(0, error);
                }
                catch
                {
                    this.state = State.End;
                }
            }
            else
            {
                this.state = State.End;
            }

            if (this.state == State.End)
            {
                this.OnEnded(error);
            }
        }

        internal void OnIoException(Exception exception)
        {
            Trace.WriteLine(TraceLevel.Error, "I/O: {0}", exception.ToString());
            if (this.state != State.End)
            {
                this.state = State.End;
                Error error = new Error() { Condition = ErrorCode.ConnectionForced };
                this.OnEnded(error);
            }
        }

        void OnEnded(Error error)
        {
            this.Error = error;

            if (this.heartBeatTimer != null)
            {
                this.heartBeatTimer.Dispose();
            }

            if (this.transport != null)
            {
                this.transport.Close();
            }

            for (int i = 0; i < this.localSessions.Length; i++)
            {
                var session = this.localSessions[i];
                if (session != null)
                {
                    session.Abort(error);
                }
            }
            
            this.NotifyClosed(error);
        }

        sealed class Pump
        {
            readonly Connection connection;

            public Pump(Connection connection)
            {
                this.connection = connection;
            }

            public void Start()
            {
                Fx.StartThread(this.PumpThread);
            }

            void PumpThread()
            {
                try
                {
                    ProtocolHeader header = Reader.ReadHeader(this.connection.transport);
                    this.connection.OnHeader(header);
                }
                catch (Exception exception)
                {
                    this.connection.OnIoException(exception);
                    return;
                }

                byte[] sizeBuffer = new byte[FixedWidth.UInt];
                while (this.connection.state != State.End)
                {
                    try
                    {
                        ByteBuffer buffer = Reader.ReadFrameBuffer(this.connection.transport, sizeBuffer, this.connection.maxFrameSize);
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
