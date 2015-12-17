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
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using Amqp.Types;

    delegate bool Condition(object state);

    public abstract class Link
    {
        public bool Role;

        public string Name;

        internal byte State;

        internal uint Handle;

        internal uint RemoteHandle;

        internal abstract void OnAttach(List fields);

        internal abstract void OnFlow(List fields);

        internal abstract void OnDisposition(uint first, uint last, DescribedValue state);
    }

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
        const uint defaultWindowSize = 100u;
        const uint minIdleTimeout = 10000;
        const uint maxIdleTimeout = 120000;
        const int maxLinks = 8;

        static int linkId;
        string host;
        int port;
        bool useSsl;
        string userName;
        string password;

        AutoResetEvent signal;
        byte state;
        NetworkStream transport;
        int maxFrameSize;

        // session state
        uint outWindow;
        uint nextOutgoingId;
        uint inWindow;
        uint nextIncomingId;
        uint deliveryId;
        Link[] links;

        uint idleTimeout;
        Timer heartBeatTimer;
        static readonly TimerCallback onHeartBeatTimer = OnHeartBeatTimer;

        public Client(string host, int port, bool useSsl, string userName, string password)
        {
            this.host = host;
            this.port = port;
            this.useSsl = useSsl;
            this.userName = userName;
            this.password = password;
            this.maxFrameSize = (int)MaxFrameSize;
            this.inWindow = defaultWindowSize;
            this.outWindow = defaultWindowSize;
            this.links = new Link[maxLinks];
            this.signal = new AutoResetEvent(false);
            this.transport = Extensions.Connect(this.host, this.port, this.useSsl);
            this.Initialize();
        }

        public Sender CreateSender(string address)
        {
            var sender = new Sender(this, Client.Name + "-sender" + Interlocked.Increment(ref linkId), address);
            lock (this)
            {
                this.AddLink(sender, false, null, address);
                return sender;
            }
        }

        public Receiver CreateReceiver(string address)
        {
            var receiver = new Receiver(this, Client.Name + "-receiver" + Interlocked.Increment(ref linkId), address);
            lock (this)
            {
                this.AddLink(receiver, true, address, null);
                return receiver;
            }
        }

        public void Close()
        {
            this.state |= CloseSent;
            this.transport.WriteFrame(0, 0, 0x18ul, new List());
            Fx.DebugPrint(true, 0, "close", null);
            try
            {
                this.Wait(o => (((Client)o).state & CloseReceived) == 0, this, 60000);
            }
            finally
            {
                this.transport.Close();
                this.ClearLinks();
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
            buffer.AdjustPosition(Extensions.TransferFramePrefixSize, 0);   // reserve space for frame header and transfer
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

                int payload = this.transport.WriteTransferFrame(sender.Handle, this.deliveryId, settled, buffer, this.maxFrameSize);
                Fx.DebugPrint(true, 0, "transfer", new List { this.deliveryId, settled, payload }, "delivery-id", "settled", "payload");
            }

            return this.deliveryId++;
        }

        internal void CloseLink(Link link)
        {
            link.State |= DetachSent;
            List detach = new List { link.Handle, true };
            this.transport.WriteFrame(0, 0, 0x16, detach);
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

            this.transport.WriteFrame(0, 0, 0x13, flow);
            Fx.DebugPrint(true, 0, "flow", flow, "next-in-id", "in-window", "next-out", "out-window", "handle", "dc", "credit");
        }

        internal void SendDisposition(bool role, uint deliveryId, bool settled, DescribedValue state)
        {
            List disposition = new List() { role, deliveryId, null, settled, state };
            this.transport.WriteFrame(0, 0, 0x15ul, disposition);
            Fx.DebugPrint(true, 0, "disposition", disposition, "role", "first", "last");
        }

        void Initialize()
        {
            byte[] header = new byte[8] { (byte)'A', (byte)'M', (byte)'Q', (byte)'P', 0, 1, 0, 0 };
            byte[] retHeader;
            if (this.userName != null)
            {
                header[4] = 3;
                this.transport.Write(header, 0, 8);
                Fx.DebugPrint(true, 0, "AMQP", new List { string.Concat(header[5], header[6], header[7]) }, header[4]);

                byte[] b1 = Encoding.UTF8.GetBytes(this.userName);
                byte[] b2 = Encoding.UTF8.GetBytes(this.password ?? string.Empty);
                byte[] b = new byte[1 + b1.Length + 1 + b2.Length];
                Array.Copy(b1, 0, b, 1, b1.Length);
                Array.Copy(b2, 0, b, b1.Length + 2, b2.Length);
                List saslInit = new List() { new Symbol("PLAIN"), b };
                this.transport.WriteFrame(1, 0, 0x41, saslInit);
                Fx.DebugPrint(true, 0, "sasl-init", saslInit, "mechanism");
                this.transport.Flush();

                retHeader = this.transport.ReadFixedSizeBuffer(8);
                Fx.DebugPrint(false, 0, "AMQP", new List { string.Concat(retHeader[5], retHeader[6], retHeader[7]) }, retHeader[4]);
                Fx.AssertAndThrow(ErrorCode.ClientInitializeHeaderCheckFailed, AreHeaderEqual(header, retHeader));

                List body = this.transport.ReadFrameBody(1, 0, 0x40);
                Fx.DebugPrint(false, 0, "sasl-mechanisms", body, "server-mechanisms");
                Fx.AssertAndThrow(ErrorCode.ClientInitializeWrongBodyCount, body.Count > 0);
                Symbol[] mechanisms = Extensions.GetSymbolMultiple(body[0]);
                Fx.AssertAndThrow(ErrorCode.ClientInitializeWrongSymbol, Array.IndexOf(mechanisms, new Symbol("PLAIN")) >= 0);

                body = this.transport.ReadFrameBody(1, 0, 0x44);
                Fx.AssertAndThrow(ErrorCode.ClientInitializeWrongBodyCount, body.Count > 0);
                Fx.DebugPrint(false, 0, "sasl-outcome", body, "code");
                Fx.AssertAndThrow(ErrorCode.ClientInitializeSaslFailed, body[0].Equals((byte)0));   // sasl-outcome.code = OK

                header[4] = 0;
            }

            this.state = OpenSent | BeginSent;
            this.transport.Write(header, 0, 8);
            Fx.DebugPrint(true, 0, "AMQP", new List { string.Concat(header[5], header[6], header[7]) }, header[4]);

            // perform open 
            var open = new List() { Guid.NewGuid().ToString(), this.host, MaxFrameSize, (ushort)0 };
            this.transport.WriteFrame(0, 0, 0x10, open);
            Fx.DebugPrint(true, 0, "open", open, "container-id", "host-name", "max-frame-size", "channel-max", "idle-time-out");

            // perform begin
            var begin = new List() { null, this.nextOutgoingId, this.inWindow, this.outWindow, (uint)(this.links.Length - 1) };
            this.transport.WriteFrame(0, 0, 0x11, begin);
            Fx.DebugPrint(true, 0, "begin", begin, "remote-channel", "next-outgoing-id", "incoming-window", "outgoing-window", "handle-max");

            retHeader = this.transport.ReadFixedSizeBuffer(8);
            Fx.DebugPrint(false, 0, "AMQP", new List { string.Concat(retHeader[5], retHeader[6], retHeader[7]) }, retHeader[4]);
            Fx.AssertAndThrow(ErrorCode.ClientInitializeHeaderCheckFailed, AreHeaderEqual(header, retHeader));
            new Thread(this.PumpThread).Start();
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
            this.transport.WriteFrame(0, 0, 0x12, attach);
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
                    this.transport.ReadFrame(out frameType, out channel, out code, out fields, out payload);
                    this.OnFrame(channel, code, fields, payload);
                }
                catch (Exception)
                {
                    this.transport.Close();
                    this.state = 0xFF;
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
                    // process open.idle-time-out
                    if (fields.Count >= 5 && fields[4] != null)
                    {
                        this.idleTimeout = (uint)fields[4];
                        if (this.idleTimeout > 0 && this.idleTimeout < uint.MaxValue)
                        {
                            Fx.AssertAndThrow(ErrorCode.ClientIdleTimeoutTooSmall, this.idleTimeout >= minIdleTimeout);
                            this.idleTimeout -= 5000;
                            if (this.idleTimeout > maxIdleTimeout)
                            {
                                this.idleTimeout = maxIdleTimeout;
                            }

                            this.heartBeatTimer = new Timer(onHeartBeatTimer, this, (int)this.idleTimeout, (int)this.idleTimeout);
                        }
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
                            this.inWindow = defaultWindowSize;
                            List flow = new List() { this.nextIncomingId, this.inWindow, this.nextOutgoingId, this.outWindow };
                            this.transport.WriteFrame(0, 0, 0x13, flow);
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
                case 0x16ul:  // dettach
                {
                    Fx.DebugPrint(false, channel, "detach", fields, "handle");
                    Link link = this.links[(uint)fields[0]];
                    Fx.AssertAndThrow(ErrorCode.ClientLinkNotFound, link != null);
                    link.State |= DetachReceived;
                    if ((link.State & DetachSent) == 0)
                    {
                        this.CloseLink(link);
                    }
                    break;
                }
                case 0x17ul:  // end
                    Fx.DebugPrint(false, channel, "end", null);
                    this.state |= EndReceived;
                    if ((this.state & EndSent) == 0)
                    {
                        this.transport.WriteFrame(0, 0, 0x17ul, new List());
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
                    }
                    break;
                default:
                    Fx.AssertAndThrow(ErrorCode.ClientInvalidCodeOnFrame, false);
                    break;
            }
        }

        static bool AreHeaderEqual(byte[] b1, byte[] b2)
        {
            // assume both are 8 bytes
            return b1[0] == b2[0] && b1[1] == b2[1] && b1[2] == b2[2] && b1[3] == b2[3]
                && b1[4] == b2[4] && b1[5] == b2[5] && b1[6] == b2[6] && b1[7] == b2[7];
        }

        static void OnHeartBeatTimer(object state)
        {
            var thisPtr = (Client)state;
            byte[] frame = new byte[] { 0, 0, 0, 8, 2, 0, 0, 0 };
            thisPtr.transport.Write(frame, 0, frame.Length);
            thisPtr.transport.Flush();
            Fx.DebugPrint(true, 0, "empty", null);
        }
    }
}