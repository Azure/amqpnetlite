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
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using Amqp.Types;
#if (MF_FRAMEWORK_VERSION_V4_2 || MF_FRAMEWORK_VERSION_V4_3 || MF_FRAMEWORK_VERSION_V4_4)
    using Microsoft.SPOT.Net.Security;
#elif (NANOFRAMEWORK_1_0)
    using System.Net.Security;
#endif

    /// <summary>
    /// The event handler that is invoked when an AMQP object is closed unexpectedly.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="link">The link. If null, the error applies to the client object.</param>
    /// <param name="error">The error condition due to which the object was closed.</param>
    public delegate void ErrorEventHandler(Client client, Link link, Symbol error);

    delegate bool Condition(object state);

    /// <summary>
    /// A Client is a channel to communicate with an AMQP peer. It manages an AMQP
    /// connection and a session within it.
    /// </summary>
    public class Client
    {
        const byte OpenSent = 1;
        const byte BeginSent = 1 << 1;
        const byte EndSent = 1 << 2;
        const byte CloseSent = 1 << 3;
        const byte OpenReceived = 1 << 4;
        const byte BeginReceived = 1 << 5;
        const byte EndReceived = 1 << 6;
        const byte CloseReceived = 1 << 7;
        const byte AttachSent = 1;
        const byte AttachReceived = 2;
        const byte DetachSent = 4;
        const byte DetachReceived = 8;
        const string Name = "netmf-lite";
        const uint MaxFrameSize = 1024;
        const uint DefaultWindowSize = 100u;
        const uint MaxIdleTimeout = 120000;
        const int MaxLinks = 8;
        const int TransferFramePrefixSize = 30;

        static readonly TimerCallback onHeartBeatTimer = OnHeartBeatTimer;
        static int linkId;
        byte state;

        // connection state
        AutoResetEvent signal;
        NetworkStream transport;
        bool sendActive;
        int maxFrameSize;
        uint idleTimeout;
        string hostName;
        Timer heartBeatTimer;

        // session state
        uint outWindow;
        uint nextOutgoingId;
        uint inWindow;
        uint nextIncomingId;
        uint deliveryId;
        Link[] links;

        /// <summary>
        /// The event that is raised when any AMQP object is closed unexpectedly.
        /// </summary>
        public event ErrorEventHandler OnError;

        /// <summary>
        /// Current idle timeout for a connection (in milliseconds).
        /// </summary>
        /// <remarks>After opening a connection the <see cref="Client"/> will keep the connection
        /// active with an heart beat timer that sends an empty packet before the value is reached.
        /// This value must be set before the <see cref="Client"/> is connected.
        /// </remarks>
        public uint IdleTimeout
        {
            get { return this.idleTimeout; }
            set
            {
                Fx.AssertAndThrow(ErrorCode.ClientNotAllowedAfterConnect, this.state == 0);
                this.idleTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the host-name to be used to open the connection. It should be set if the
        /// virtual host is different from the network host in Connect method.
        /// </summary>
        public string HostName
        {
            get { return this.hostName; }
            set
            {
                Fx.AssertAndThrow(ErrorCode.ClientNotAllowedAfterConnect, this.state == 0);
                this.hostName = value;
            }
        }

        /// <summary>
        /// Creates a new Client object with default settings.
        /// </summary>
        public Client()
        {
            this.maxFrameSize = (int)MaxFrameSize;
            this.idleTimeout = uint.MaxValue;   // no idle timeout
            this.inWindow = DefaultWindowSize;
            this.outWindow = DefaultWindowSize;
            this.links = new Link[MaxLinks];
            this.signal = new AutoResetEvent(false);
        }

        /// <summary>
        /// Establish a connection and a session for the client.
        /// </summary>
        /// <param name="host">The network host name or IP address to connect.</param>
        /// <param name="port">The port to connect.</param>
        /// <param name="useSsl">If true, use secure socket.</param>
        /// <param name="userName">If set, the user name for authentication.</param>
        /// <param name="password">If set, the password for authentication.</param>
        public void Connect(string host, int port, bool useSsl, string userName, string password)
        {
            this.transport = Connect(host, port, useSsl);

            byte[] header = new byte[8] { (byte)'A', (byte)'M', (byte)'Q', (byte)'P', 0, 1, 0, 0 };
            byte[] retHeader;
            if (userName != null)
            {
                header[4] = 3;
                this.transport.Write(header, 0, 8);
                Fx.DebugPrint(true, 0, "AMQP", new List { string.Concat(header[5], header[6], header[7]) }, header[4]);

                byte[] b1 = Encoding.UTF8.GetBytes(userName);
                byte[] b2 = Encoding.UTF8.GetBytes(password ?? string.Empty);
                byte[] b = new byte[1 + b1.Length + 1 + b2.Length];
                Array.Copy(b1, 0, b, 1, b1.Length);
                Array.Copy(b2, 0, b, b1.Length + 2, b2.Length);
                List saslInit = new List() { new Symbol("PLAIN"), b };
                this.WriteFrame(1, 0, 0x41, saslInit);
                Fx.DebugPrint(true, 0, "sasl-init", saslInit, "mechanism");
                this.transport.Flush();

                retHeader = this.ReadFixedSizeBuffer(8);
                Fx.DebugPrint(false, 0, "AMQP", new List { string.Concat(retHeader[5], retHeader[6], retHeader[7]) }, retHeader[4]);
                Fx.AssertAndThrow(ErrorCode.ClientInitializeHeaderCheckFailed, AreHeaderEqual(header, retHeader));

                List body = this.ReadFrameBody(1, 0, 0x40);
                Fx.DebugPrint(false, 0, "sasl-mechanisms", body, "server-mechanisms");
                Fx.AssertAndThrow(ErrorCode.ClientInitializeWrongBodyCount, body.Count > 0);
                Symbol[] mechanisms = GetSymbolMultiple(body[0]);
                Fx.AssertAndThrow(ErrorCode.ClientInitializeWrongSymbol, Array.IndexOf(mechanisms, new Symbol("PLAIN")) >= 0);

                body = this.ReadFrameBody(1, 0, 0x44);
                Fx.AssertAndThrow(ErrorCode.ClientInitializeWrongBodyCount, body.Count > 0);
                Fx.DebugPrint(false, 0, "sasl-outcome", body, "code");
                Fx.AssertAndThrow(ErrorCode.ClientInitializeSaslFailed, body[0].Equals((byte)0));   // sasl-outcome.code = OK

                header[4] = 0;
            }

            this.transport.Write(header, 0, 8);
            Fx.DebugPrint(true, 0, "AMQP", new List { string.Concat(header[5], header[6], header[7]) }, header[4]);

            // perform open 
            this.state |= OpenSent;
            var open = new List() { Guid.NewGuid().ToString(), this.hostName ?? host, MaxFrameSize, (ushort)0 };
            this.WriteFrame(0, 0, 0x10, open);
            Fx.DebugPrint(true, 0, "open", open, "container-id", "host-name", "max-frame-size", "channel-max", "idle-time-out");

            // perform begin
            this.state |= BeginSent;
            var begin = new List() { null, this.nextOutgoingId, this.inWindow, this.outWindow, (uint)(this.links.Length - 1) };
            this.WriteFrame(0, 0, 0x11, begin);
            Fx.DebugPrint(true, 0, "begin", begin, "remote-channel", "next-outgoing-id", "incoming-window", "outgoing-window", "handle-max");

            retHeader = this.ReadFixedSizeBuffer(8);
            Fx.DebugPrint(false, 0, "AMQP", new List { string.Concat(retHeader[5], retHeader[6], retHeader[7]) }, retHeader[4]);
            Fx.AssertAndThrow(ErrorCode.ClientInitializeHeaderCheckFailed, AreHeaderEqual(header, retHeader));
            new Thread(this.PumpThread).Start();
        }
        
        /// <summary>
        /// Creates a Sender from the client.
        /// </summary>
        /// <param name="address">The address of the node where messages are sent.</param>
        /// <returns>A Sender object.</returns>
        public Sender CreateSender(string address)
        {
            Fx.AssertAndThrow(ErrorCode.ClientNotConnected, this.state > 0 && this.state < 0xff);
            var sender = new Sender(this, Client.Name + "-sender" + Interlocked.Increment(ref linkId), address);
            lock (this)
            {
                this.AddLink(sender, false, null, address);
                return sender;
            }
        }

        /// <summary>
        /// Creates a Receiver from the client.
        /// </summary>
        /// <param name="address">The address of the node where messages are received.</param>
        /// <returns>A Receiver object.</returns>
        public Receiver CreateReceiver(string address)
        {
            Fx.AssertAndThrow(ErrorCode.ClientNotConnected, this.state > 0 && this.state < 0xff);
            var receiver = new Receiver(this, Client.Name + "-receiver" + Interlocked.Increment(ref linkId), address);
            lock (this)
            {
                this.AddLink(receiver, true, address, null);
                return receiver;
            }
        }

        /// <summary>
        /// Close the client.
        /// </summary>
        public void Close()
        {
            try
            {
                if (this.state > 0 && this.state < 0xff)
                {
                    this.state |= CloseSent;
                    this.WriteFrame(0, 0, 0x18ul, new List());
                    Fx.DebugPrint(true, 0, "close", null);
                    this.Wait(o => (((Client)o).state & CloseReceived) == 0, this, 60000);
                }
            }
            finally
            {
                if (this.transport != null)
                {
                    this.transport.Close();
                }

                this.ClearLinks();

                if (this.heartBeatTimer != null)
                {
                    // kill heart beat timer
                    this.heartBeatTimer.Dispose();
                }
            }
        }

        internal void Wait(Condition condition, object state, int millisecondsTimeout)
        {
            while (condition(state))
            {
                Fx.AssertAndThrow(ErrorCode.ClientWaitTimeout, this.signal.WaitOne(millisecondsTimeout, false));
            }
        }

        internal uint Send(Sender sender, Message message, bool settled)
        {
            ByteBuffer buffer = new ByteBuffer(128, true);
            buffer.AdjustPosition(TransferFramePrefixSize, 0);   // reserve space for frame header and transfer
            message.Encode(buffer);

            while (buffer.Length > 0)
            {
                this.Wait(o => ((Client)o).outWindow == 0, this, 60000);
                lock (this)
                {
                    this.nextOutgoingId++;
                    if (this.outWindow < uint.MaxValue)
                    {
                        this.outWindow--;
                    }
                }

                int payload = this.WriteTransferFrame(sender.Handle, this.deliveryId, settled, buffer, this.maxFrameSize);
                Fx.DebugPrint(true, 0, "transfer", new List { this.deliveryId, settled, payload }, "delivery-id", "settled", "payload");
            }

            return this.deliveryId++;
        }

        internal void CloseLink(Link link)
        {
            link.State |= DetachSent;
            List detach = new List { link.Handle, true };
            this.WriteFrame(0, 0, 0x16, detach);
            Fx.DebugPrint(true, 0, "detach", detach, "handle");
            try
            {
                this.Wait(o => (((Link)o).State & DetachReceived) == 0, link, 60000);
            }
            finally
            {
                link.State = 0xff;
                int index = (int)link.RemoteHandle;
                lock (this)
                {
                    if (index >= 0)
                    {
                        this.links[index] = null;
                    }
                    else
                    {
                        this.links[~index] = null;
                    }
                }
            }
        }

        internal void SendFlow(uint handle, uint dc, uint credit)
        {
            List flow;
            lock (this)
            {
                flow = new List() { this.nextIncomingId, this.inWindow, this.nextOutgoingId, this.outWindow, handle, dc, credit };
            }

            this.WriteFrame(0, 0, 0x13, flow);
            Fx.DebugPrint(true, 0, "flow", flow, "next-in-id", "in-window", "next-out", "out-window", "handle", "dc", "credit");
        }

        internal void SendDisposition(bool role, uint deliveryId, bool settled, DescribedValue state)
        {
            List disposition = new List() { role, deliveryId, null, settled, state };
            this.WriteFrame(0, 0, 0x15ul, disposition);
            Fx.DebugPrint(true, 0, "disposition", disposition, "role", "first", "last");
        }

        void RaiseErrorEvent(Link link, Symbol error)
        {
            var onError = this.OnError;
            if (onError != null)
            {
                onError.Invoke(this, link, error);
            }
        }

        uint GetLinkHandle(out int index)
        {
            index = -1;
            int bits = 0;
            for (int i = 0; i < this.links.Length; i++)
            {
                if (this.links[i] != null)
                {
                    bits |= (1 << (int)this.links[i].Handle);
                }
                else if (index < 0)
                {
                    index = i;
                }
            }

            Fx.AssertAndThrow(ErrorCode.ClientNoHandleAvailable, index >= 0);
            uint localHandle = 0;
            while ((bits & 1) > 0)
            {
                localHandle++;
                bits >>= 1;
            }

            return localHandle;
        }

        void AddLink(Link link, bool role, string source, string target)
        {
            int index;
            link.Handle = this.GetLinkHandle(out index);
            link.RemoteHandle = (uint)~index;
            this.links[index] = link;

            link.State = AttachSent;
            List attach = new List()
            {
                link.Name, link.Handle, link.Role, null, null,
                new DescribedValue(0x28ul, new List() { source }),
                new DescribedValue(0x29ul, new List() { target })
            };

            this.WriteFrame(0, 0, 0x12, attach);
            Fx.DebugPrint(true, 0, "attach", attach, "name", "handle", "role", "snd-mode", "rcv-mode", "source", "target");
        }

        void ClearLinks()
        {
            lock (this)
            {
                for (int i = 0; i < this.links.Length; i++)
                {
                    if (this.links[i] != null)
                    {
                        this.links[i].State = 0xff;
                        this.links[i] = null;
                    }
                }
            }
        }

        void PumpThread()
        {
            byte frameType;
            ushort channel;
            ulong code = 0;
            List fields = null;
            ByteBuffer payload = null;
            while (this.state < 0xFF)
            {
                try
                {
                    this.ReadFrame(out frameType, out channel, out code, out fields, out payload);
                    this.OnFrame(channel, code, fields, payload);
                }
                catch (Exception)
                {
                    this.transport.Close();
                    this.state = 0xFF;
                    this.Close();
                    this.RaiseErrorEvent(null, new Symbol("amqp:connection:reset"));
                }

                this.signal.Set();
            }
        }

        void OnFrame(ushort channel, ulong code, List fields, ByteBuffer payload)
        {
            switch (code)
            {
                case 0x10ul:  // open
                    Fx.DebugPrint(false, channel, "open", fields, "container-id", "host-name", "max-frame-size", "channel-max", "idle-time-out");
                    this.state |= OpenReceived;

                    // process open.idle-time-out if exists: final value is determined by the min of the local and the remote values
                    uint remoteValue = uint.MaxValue;
                    if (fields.Count >= 5 && fields[4] != null)
                    {
                        remoteValue = (uint)fields[4];
                    }
                    uint timeout = this.idleTimeout < remoteValue ? this.idleTimeout : remoteValue;
                    if (timeout < uint.MaxValue)
                    {
                        timeout -= 5000;
                        timeout = timeout > MaxIdleTimeout ? MaxIdleTimeout : timeout;
                        this.heartBeatTimer = new Timer(onHeartBeatTimer, this, (int)timeout, (int)timeout);
                    }
                    break;
                case 0x11ul:  // begin
                    Fx.DebugPrint(false, channel, "begin", fields, "remote-channel", "next-outgoing-id", "incoming-window", "outgoing-window", "handle-max");
                    this.nextIncomingId = (uint)fields[1];
                    this.outWindow = (uint)fields[2];
                    this.state |= BeginReceived;
                    break;
                case 0x12ul:  // attach
                {
                    Fx.DebugPrint(false, channel, "attach", fields, "name", "handle", "role", "snd-mode", "rcv-mode", "source", "target");
                    Link link = null;
                    uint remoteHandle = (uint)fields[1];
                    Fx.AssertAndThrow(ErrorCode.ClientInvalidHandle, remoteHandle < this.links.Length);
                    lock (this)
                    {
                        for (int i = 0; i < this.links.Length; i++)
                        {
                            if (this.links[i] != null && this.links[i].Name.Equals(fields[0]))
                            {
                                link = this.links[i];
                                int index = (int)~link.RemoteHandle;
                                Fx.AssertAndThrow(ErrorCode.ClientInvalidHandle, index == i);
                                if (index != (int)remoteHandle)
                                {
                                    Fx.AssertAndThrow(ErrorCode.ClientHandlInUse, this.links[(int)remoteHandle] == null);
                                    this.links[(int)remoteHandle] = link;
                                    this.links[i] = null;
                                }
                                break;
                            }
                        }
                    }
                    Fx.AssertAndThrow(ErrorCode.ClientLinkNotFound, link != null);
                    link.RemoteHandle = remoteHandle;
                    link.State |= AttachReceived;
                    link.OnAttach(fields);
                    break;
                }
                case 0x13ul:  // flow
                {
                    Fx.DebugPrint(false, channel, "flow", fields, "next-in-id", "in-window", "next-out", "out-window", "handle", "dc", "credit");
                    uint nextIncomingId = (uint)fields[0];
                    uint incomingWindow = (uint)fields[1];
                    lock(this)
                    {
                        this.outWindow = incomingWindow < uint.MaxValue ?
                            nextIncomingId + incomingWindow - this.nextOutgoingId :
                            uint.MaxValue;
                    }

                    if (fields[4] != null)
                    {
                        Link link = this.links[(uint)fields[4]];
                        Fx.AssertAndThrow(ErrorCode.ClientLinkNotFound, link != null);
                        link.OnFlow(fields);
                    }
                    break;
                }
                case 0x14ul:  // transfer
                {
                    lock (this)
                    {
                        this.nextOutgoingId++;
                        if (--this.inWindow == 0)
                        {
                            this.inWindow = DefaultWindowSize;
                            List flow = new List() { this.nextIncomingId, this.inWindow, this.nextOutgoingId, this.outWindow };
                            this.WriteFrame(0, 0, 0x13, flow);
                            Fx.DebugPrint(true, 0, "flow", flow, "next-in-id", "in-window", "next-out", "out-window", "handle", "dc", "credit");
                        }
                    }

                    Link link = this.links[(uint)fields[0]];
                    Fx.AssertAndThrow(ErrorCode.ClientLinkNotFound, link != null);
                    ((Receiver)link).OnTransfer(fields, payload);
                    break;
                }
                case 0x15ul:  // disposition
                {
                    Fx.DebugPrint(false, channel, "disposition", fields, "role", "first", "last");
                    bool role = (bool)fields[0];
                    uint first = (uint)fields[1];
                    uint last = fields[2] == null ? first : (uint)fields[2];
                    for (int i = 0; i < this.links.Length; i++)
                    {
                        Link link = this.links[i];
                        if (link != null && role != link.Role)
                        {
                            link.OnDisposition(first, last, fields[4] as DescribedValue);
                        }
                    }
                    break;
                }
                case 0x16ul:  // detach
                {
                    Fx.DebugPrint(false, channel, "detach", fields, "handle");
                    Link link = this.links[(uint)fields[0]];
                    Fx.AssertAndThrow(ErrorCode.ClientLinkNotFound, link != null);
                    link.State |= DetachReceived;
                    if ((link.State & DetachSent) == 0)
                    {
                        this.CloseLink(link);
                        this.RaiseErrorEvent(link, GetError(fields, 2));
                    }
                    break;
                }
                case 0x17ul:  // end
                    Fx.DebugPrint(false, channel, "end", null);
                    this.state |= EndReceived;
                    if ((this.state & EndSent) == 0)
                    {
                        this.WriteFrame(0, 0, 0x17ul, new List());
                        Fx.DebugPrint(true, channel, "end", null);
                        this.ClearLinks();
                    }
                    break;
                case 0x18ul:  // close
                    Fx.DebugPrint(false, channel, "close", null);
                    this.state |= CloseReceived;
                    if ((this.state & CloseSent) == 0)
                    {
                        this.Close();
                        this.RaiseErrorEvent(null, GetError(fields, 0));
                    }
                    break;
                default:
                    Fx.AssertAndThrow(ErrorCode.ClientInvalidCodeOnFrame, false);
                    break;
            }
        }

        void WriteFrame(byte frameType, ushort channel, ulong code, List fields)
        {
            ByteBuffer buffer = new ByteBuffer(64, true);

            // frame header
            buffer.Append(FixedWidth.UInt);
            AmqpBitConverter.WriteUByte(buffer, 2);
            AmqpBitConverter.WriteUByte(buffer, (byte)frameType);
            AmqpBitConverter.WriteUShort(buffer, channel);

            // command
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Described);
            Encoder.WriteULong(buffer, code, true);
            AmqpBitConverter.WriteUByte(buffer, FormatCode.List32);
            int sizeOffset = buffer.WritePos;
            buffer.Append(8);
            AmqpBitConverter.WriteInt(buffer.Buffer, sizeOffset + 4, fields.Count);
            for (int i = 0; i < fields.Count; i++)
            {
                Encoder.WriteObject(buffer, fields[i]);
            }

            AmqpBitConverter.WriteInt(buffer.Buffer, sizeOffset, buffer.Length - sizeOffset);
            AmqpBitConverter.WriteInt(buffer.Buffer, 0, buffer.Length); // frame size
            this.transport.Write(buffer.Buffer, buffer.Offset, buffer.Length);
            this.sendActive = true;
        }

        int WriteTransferFrame(uint handle, uint deliveryId, bool settled, ByteBuffer buffer, int maxFrameSize)
        {
            // payload should have bytes reserved for frame header and transfer
            int frameSize = Math.Min(buffer.Length + TransferFramePrefixSize, maxFrameSize);
            int payloadSize = frameSize - TransferFramePrefixSize;
            int offset = buffer.Offset - TransferFramePrefixSize;
            int pos = offset;

            // frame size
            buffer.Buffer[pos++] = (byte)(frameSize >> 24);
            buffer.Buffer[pos++] = (byte)(frameSize >> 16);
            buffer.Buffer[pos++] = (byte)(frameSize >> 8);
            buffer.Buffer[pos++] = (byte)frameSize;

            // DOF, type and channel
            buffer.Buffer[pos++] = 0x02;
            buffer.Buffer[pos++] = 0x00;
            buffer.Buffer[pos++] = 0x00;
            buffer.Buffer[pos++] = 0x00;

            // transfer(list8-size,count)
            buffer.Buffer[pos++] = 0x00;
            buffer.Buffer[pos++] = 0x53;
            buffer.Buffer[pos++] = 0x14;
            buffer.Buffer[pos++] = 0xc0;
            buffer.Buffer[pos++] = 0x10;
            buffer.Buffer[pos++] = 0x06;

            buffer.Buffer[pos++] = 0x52; // handle
            buffer.Buffer[pos++] = (byte)handle;

            buffer.Buffer[pos++] = 0x70; // delivery id: uint
            buffer.Buffer[pos++] = (byte)(deliveryId >> 24);
            buffer.Buffer[pos++] = (byte)(deliveryId >> 16);
            buffer.Buffer[pos++] = (byte)(deliveryId >> 8);
            buffer.Buffer[pos++] = (byte)deliveryId;

            buffer.Buffer[pos++] = 0xa0; // delivery tag: bin8
            buffer.Buffer[pos++] = 0x04;
            buffer.Buffer[pos++] = (byte)(deliveryId >> 24);
            buffer.Buffer[pos++] = (byte)(deliveryId >> 16);
            buffer.Buffer[pos++] = (byte)(deliveryId >> 8);
            buffer.Buffer[pos++] = (byte)deliveryId;

            buffer.Buffer[pos++] = 0x43; // message-format
            buffer.Buffer[pos++] = settled ? (byte)0x41 : (byte)0x42;   // settled
            buffer.Buffer[pos++] = buffer.Length > payloadSize ? (byte)0x41 : (byte)0x42;   // more

            this.transport.Write(buffer.Buffer, offset, frameSize);
            this.sendActive = true;
            buffer.Complete(payloadSize);

            return payloadSize;
        }

        void ReadFrame(out byte frameType, out ushort channel, out ulong code, out List fields, out ByteBuffer payload)
        {
            byte[] headerBuffer = this.ReadFixedSizeBuffer(8);
            int size = AmqpBitConverter.ReadInt(headerBuffer, 0);
            frameType = headerBuffer[5];    // TODO: header EXT
            channel = (ushort)(headerBuffer[6] << 8 | headerBuffer[7]);

            size -= 8;
            if (size > 0)
            {
                byte[] frameBuffer = this.ReadFixedSizeBuffer(size);
                ByteBuffer buffer = new ByteBuffer(frameBuffer, 0, size, size);
                Fx.AssertAndThrow(ErrorCode.ClientInvalidFormatCodeRead, Encoder.ReadFormatCode(buffer) == FormatCode.Described);

                code = Encoder.ReadULong(buffer, Encoder.ReadFormatCode(buffer));
                fields = Encoder.ReadList(buffer, Encoder.ReadFormatCode(buffer));
                if (buffer.Length > 0)
                {
                    payload = new ByteBuffer(buffer.Buffer, buffer.Offset, buffer.Length, buffer.Length);
                }
                else
                {
                    payload = null;
                }
            }
            else
            {
                code = 0;
                fields = null;
                payload = null;
            }
        }

        List ReadFrameBody(byte frameType, ushort channel, ulong code)
        {
            byte t;
            ushort c;
            ulong d;
            List f;
            ByteBuffer p;
            this.ReadFrame(out t, out c, out d, out f, out p);
            Fx.AssertAndThrow(ErrorCode.ClientInvalidFrameType, t == frameType);
            Fx.AssertAndThrow(ErrorCode.ClientInvalidChannel, c == channel);
            Fx.AssertAndThrow(ErrorCode.ClientInvalidCode, d == code);
            Fx.AssertAndThrow(ErrorCode.ClientInvalidFieldList, f != null);
            Fx.AssertAndThrow(ErrorCode.ClientInvalidPayload, p == null);
            return f;
        }

        byte[] ReadFixedSizeBuffer(int size)
        {
            byte[] buffer = new byte[size];
            int offset = 0;
            while (size > 0)
            {
                int bytes = this.transport.Read(buffer, offset, size);
                offset += bytes;
                size -= bytes;
            }

            return buffer;
        }

        static NetworkStream Connect(string host, int port, bool useSsl)
        {
            var ipHostEntry = Dns.GetHostEntry(host);
            Socket socket = null;
            SocketException exception = null;
            foreach (var ipAddress in ipHostEntry.AddressList)
            {
                if (ipAddress == null) continue;

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    socket.Connect(new IPEndPoint(ipAddress, port));
                    exception = null;
                    break;
                }
                catch (SocketException socketException)
                {
                    exception = socketException;
                    socket = null;
                }
            }

            if (exception != null)
            {
                throw exception;
            }

            NetworkStream stream;
            if (useSsl)
            {
                SslStream sslStream = new SslStream(socket);

              #if (MF_FRAMEWORK_VERSION_V4_2 || MF_FRAMEWORK_VERSION_V4_3 || MF_FRAMEWORK_VERSION_V4_4)
                sslStream.AuthenticateAsClient(host, null, SslVerification.VerifyPeer, SslProtocols.TLSv1);
#elif (NANOFRAMEWORK_1_0)
                sslStream.AuthenticateAsClient(host, null, SslProtocols.Tls11);
#endif

                stream = sslStream;
            }
            else
            {
                stream = new NetworkStream(socket, true);
            }

            return stream;
        }

        static Symbol[] GetSymbolMultiple(object multiple)
        {
            Symbol[] array = multiple as Symbol[];
            if (array != null)
            {
                return array;
            }

            Symbol symbol = multiple as Symbol;
            if (symbol != null)
            {
                return new Symbol[] { symbol };
            }

            throw new Exception("object is not a multiple type");
        }
        
        static bool AreHeaderEqual(byte[] b1, byte[] b2)
        {
            // assume both are 8 bytes
            return b1[0] == b2[0] && b1[1] == b2[1] && b1[2] == b2[2] && b1[3] == b2[3]
                && b1[4] == b2[4] && b1[5] == b2[5] && b1[6] == b2[6] && b1[7] == b2[7];
        }

        static Symbol GetError(List fields, int errorIndex)
        {
            if (fields.Count > errorIndex && fields[errorIndex] != null)
            {
                var dv = fields[errorIndex] as DescribedValue;
                if (dv != null && dv.Descriptor.Equals(0x1dul))
                {
                    List error = (List)dv.Value;
                    if (error.Count > 0)
                    {
                        return (Symbol)error[0];
                    }
                }
            }

            return null;
        }

        static void OnHeartBeatTimer(object state)
        {
            var thisPtr = (Client)state;
            if (!thisPtr.sendActive)
            {
                try
                {
                    byte[] frame = new byte[] { 0, 0, 0, 8, 2, 0, 0, 0 };
                    thisPtr.transport.Write(frame, 0, frame.Length);
                    thisPtr.transport.Flush();
                    Fx.DebugPrint(true, 0, "empty", null);
                }
                catch
                {
                    thisPtr.state = 0xff;
                    thisPtr.Close();
                }
            }

            thisPtr.sendActive = false;
        }
    }
}