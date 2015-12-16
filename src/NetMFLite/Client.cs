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

        internal const byte AttachSent = 1;
        internal const byte AttachReceived = 2;
        internal const byte DetachSent = 4;
        internal const byte DetachReceived = 8;

        internal const string Name = "netmf-lite";
        internal const int MaxFrameSize = 1024;
        const uint defaultWindowSize = 100u;
        const uint minIdleTimeout = 10000;
        const uint maxIdleTimeout = 120000;

        string host;
        int port;
        bool useSsl;
        string userName;
        string password;

        internal AutoResetEvent signal;
        internal byte state;
        internal NetworkStream transport;
        internal int maxFrameSize = MaxFrameSize;

        internal Sender sender;
        internal uint outWindow = defaultWindowSize;
        internal uint nextOutgoingId;

        internal Receiver receiver;
        internal uint inWindow = defaultWindowSize;
        internal uint nextIncomingId;

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
            this.signal = new AutoResetEvent(false);
            this.transport = Extensions.Connect(this.host, this.port, this.useSsl);
            this.Initialize();
        }

        public Sender GetSender(string address)
        {
            Fx.AssertAndThrow(ErrorCode.ClientSenderIsNull, this.sender == null);
            return this.sender = new Sender(this, address);
        }

        public Receiver GetReceiver(string address)
        {
            Fx.AssertAndThrow(ErrorCode.ClientReceiverIsNull, this.receiver == null);
            return this.receiver = new Receiver(this, address);
        }

        public void Close()
        {
            this.state |= CloseSent;
            this.transport.WriteFrame(0, 0, 0x18ul, new List());
            Fx.DebugPrint(true, 0, "close", null);
            this.Wait(o => (((Client)o).state & CloseReceived) == 0, this, 60000);
            this.transport.Close();
        }

        internal void Wait(Condition condition, object state, int millisecondsTimeout)
        {
            while (condition(state))
            {
                Fx.AssertAndThrow(ErrorCode.ClientWaitTimeout, this.signal.WaitOne(millisecondsTimeout, false));
            }
        }

        internal void Send(Message message, uint deliveryId, bool settled)
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

                int payload = this.transport.WriteTransferFrame(deliveryId, settled, buffer, this.maxFrameSize);
                Fx.DebugPrint(true, 0, "transfer", new List { deliveryId, settled, payload }, "delivery-id", "settled", "payload");
            }
        }

        internal void SendAttach(string name, uint handle, bool role, string sourceAddress, string targetAddress)
        {
            List attach = Attach(name, handle, role, sourceAddress, targetAddress);
            this.transport.WriteFrame(0, 0, 0x12, attach);
            Fx.DebugPrint(true, 0, "attach", attach, "name", "handle", "role", "snd-mode", "rcv-mode", "source", "target");
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

        void Initialize()
        {
            byte[] header = new byte[8] { (byte)'A', (byte)'M', (byte)'Q', (byte)'P', 0, 1, 0, 0 };
            byte[] retHeader;
            if (this.userName != null)
            {
                header[4] = 3;
                this.transport.Write(header, 0, 8);
                Fx.DebugPrint(true, 0, "AMQP", new List { string.Concat(header[5], header[6], header[7]) }, header[4]);
                List saslInit = SaslInit("PLAIN", this.userName, this.password);
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
            var open = Open(Guid.NewGuid().ToString(), this.host, MaxFrameSize, 0);
            this.transport.WriteFrame(0, 0, 0x10, open);
            Fx.DebugPrint(true, 0, "open", open, "container-id", "host-name", "max-frame-size", "channel-max", "idle-time-out");

            // perform begin
            var begin = Begin(this.nextOutgoingId, this.inWindow, this.outWindow);
            this.transport.WriteFrame(0, 0, 0x11, begin);
            Fx.DebugPrint(true, 0, "begin", begin, "remote-channel", "next-outgoing-id", "incoming-window", "outgoing-window", "handle-max");

            retHeader = this.transport.ReadFixedSizeBuffer(8);
            Fx.DebugPrint(false, 0, "AMQP", new List { string.Concat(retHeader[5], retHeader[6], retHeader[7]) }, retHeader[4]);
            Fx.AssertAndThrow(ErrorCode.ClientInitializeHeaderCheckFailed, AreHeaderEqual(header, retHeader));
            new Thread(this.PumpThread).Start();
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
                case 0x10:  // open
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
                case 0x11:  // begin
                    Fx.DebugPrint(false, channel, "begin", fields, "remote-channel", "next-outgoing-id", "incoming-window", "outgoing-window", "handle-max");
                    this.nextIncomingId = (uint)fields[1];
                    this.outWindow = (uint)fields[2];
                    this.state |= BeginReceived;
                    break;
                case 0x12:  // attach
                {
                    Fx.DebugPrint(false, channel, "attach", fields, "name", "handle", "role", "snd-mode", "rcv-mode", "source", "target");
                    bool role = (bool)fields[2];
                    if (role)
                    {
                        Fx.AssertAndThrow(ErrorCode.ClientAttachSenderIsNull, this.sender != null);
                        this.sender.OnAttach(fields);
                    }
                    else
                    {
                        Fx.AssertAndThrow(ErrorCode.ClientAttachReceiverIsNull, this.receiver != null);
                        this.receiver.OnAttach(fields);
                    }
                    break;
                }
                case 0x13:  // flow
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

                    Sender sender = this.sender;
                    if (fields[4] != null && sender != null)
                    {
                        sender.OnFlow(fields);
                    }
                    break;
                }
                case 0x14:  // transfer
                {
                    List flow = null;
                    lock (this)
                    {
                        this.nextOutgoingId++;
                        if (--this.inWindow == 0)
                        {
                            this.inWindow = defaultWindowSize;
                            flow = new List() { this.nextIncomingId, this.inWindow, this.nextOutgoingId, this.outWindow };
                            this.transport.WriteFrame(0, 0, 0x13, flow);
                            Fx.DebugPrint(true, 0, "flow", flow, "next-in-id", "in-window", "next-out", "out-window", "handle", "dc", "credit");
                        }
                    }

                    if (this.receiver != null)
                    {
                        this.receiver.OnTransfer(fields, payload);
                    }
                    break;
                }
                case 0x15:  // disposition
                {
                    Fx.DebugPrint(false, channel, "disposition", fields, "role", "first", "last");
                    bool role = (bool)fields[0];
                    if (role)
                    {
                        this.sender.OnDisposition(fields);
                    }
                    break;
                }
                case 0x16:  // dettach
                {
                    Fx.DebugPrint(false, channel, "detach", fields, "handle");
                    uint handle = (uint)fields[0];

                    if (this.sender != null && handle == this.sender.remoteHandle)
                    {
                        this.sender.OnDetach(fields);
                    }
                    else if (this.receiver != null && handle == this.receiver.remoteHandle)
                    {
                        this.receiver.OnDetach(fields);
                    }
                    break;
                }
                case 0x17:  // end
                    Fx.DebugPrint(false, channel, "end", null);
                    this.state |= EndReceived;
                    break;
                case 0x18:  // close
                    Fx.DebugPrint(false, channel, "close", null);
                    this.state |= CloseReceived;
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

        static List SaslInit(string mechanism, string userName, string password)
        {
            byte[] b1 = Encoding.UTF8.GetBytes(userName);
            byte[] b2 = Encoding.UTF8.GetBytes(password ?? string.Empty);
            byte[] b = new byte[1 + b1.Length + 1 + b2.Length];
            Array.Copy(b1, 0, b, 1, b1.Length);
            Array.Copy(b2, 0, b, b1.Length + 2, b2.Length);
            return new List() { new Symbol("PLAIN"), b };
        }

        static List Open(string containerId, string hostName, uint maxFrameSize, ushort channelMax)
        {
            return new List() { containerId, hostName, maxFrameSize, channelMax };
        }

        static List Begin(uint nextOutgoingId, uint inWindow, uint outWindow)
        {
            return new List() { null, nextOutgoingId, inWindow, outWindow, 1u };
        }

        internal static List Attach(string name, uint handle, bool role, string sourceAddress, string targetAddress)
        {
            return new List() { name, handle, role, null, null, new DescribedValue(0x28ul, new List() { sourceAddress }),
                new DescribedValue(0x29ul, new List() { targetAddress }), null, null, 0u};
        }

        internal static List Detach(uint handle)
        {
            return new List { handle, true };
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