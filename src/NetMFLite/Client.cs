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
        internal const int MaxFrameSize = 16 * 1024;
        const uint window = 100u;

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
        internal uint outWindow;
        internal uint nextOutgoingId;

        internal Receiver receiver;
        internal uint inWindow;
        internal uint nextIncomingId;

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
            Fx.AssertAndThrow(1000, this.sender == null);
            return this.sender = new Sender(this, address);
        }

        public Receiver GetReceiver(string address)
        {
            Fx.AssertAndThrow(1000, this.receiver == null);
            this.inWindow = window;
            return this.receiver = new Receiver(this, address);
        }

        public void Close()
        {
            this.state |= CloseSent;
            this.transport.WriteFrame(0, 0, 0x18ul, new List());
            this.Wait(o => (((Client)o).state & CloseReceived) == 0, this, 60000);
            this.transport.Close();
        }

        internal void Wait(Condition condition, object state, int millisecondsTimeout)
        {
            while (condition(state))
            {
                Fx.AssertAndThrow(1000, this.signal.WaitOne(millisecondsTimeout, false));
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

                this.transport.WriteTransferFrame(deliveryId, settled, buffer, this.maxFrameSize);
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
        }

        void Initialize()
        {
            byte[] header = new byte[8] { (byte)'A', (byte)'M', (byte)'Q', (byte)'P', 0, 1, 0, 0 };
            byte[] retHeader;
            if (this.userName != null)
            {
                header[4] = 3;
                this.transport.Write(header, 0, 8);
                this.transport.WriteFrame(1, 0, 0x41, SaslInit("PLAIN", this.userName, this.password));
                this.transport.Flush();

                retHeader = this.transport.ReadFixedSizeBuffer(8);
                Fx.AssertAndThrow(2000, AreHeaderEqual(header, retHeader));
                List body = this.transport.ReadFrameBody(1, 0, 0x40);
                Fx.AssertAndThrow(2001, body.Count > 0);
                Symbol[] mechanisms = Extensions.GetSymbolMultiple(body[0]);
                Fx.AssertAndThrow(2002, Array.IndexOf(mechanisms, new Symbol("PLAIN")) >= 0);

                body = this.transport.ReadFrameBody(1, 0, 0x44);
                Fx.AssertAndThrow(2003, body.Count > 0);
                Fx.AssertAndThrow(2004, body[0].Equals((byte)0));   // sasl-outcome.code = OK

                header[4] = 0;
            }

            this.state = OpenSent | BeginSent;
            this.transport.Write(header, 0, 8);
            this.transport.WriteFrame(0, 0, 0x10, Open(Guid.NewGuid().ToString(), this.host, 8 * 1024, 0));
            this.transport.WriteFrame(0, 0, 0x11, Begin(this.nextOutgoingId, this.inWindow));

            retHeader = this.transport.ReadFixedSizeBuffer(8);
            Fx.AssertAndThrow(2000, AreHeaderEqual(header, retHeader));
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
                    this.OnFrame(code, fields, payload);
                }
                catch (Exception)
                {
                    this.transport.Close();
                    this.state = 0xFF;
                }

                this.signal.Set();
            }
        }

        void OnFrame(ulong code, List fields, ByteBuffer payload)
        {
            switch (code)
            {
                case 0x10:  // open
                    this.state |= OpenReceived;
                    break;
                case 0x11:  // begin
                    this.nextIncomingId = (uint)fields[1];
                    this.outWindow = (uint)fields[3];
                    this.state |= BeginReceived;
                    break;
                case 0x12:  // attach
                {
                    bool role = (bool)fields[2];
                    if (role)
                    {
                        Fx.AssertAndThrow(1000, this.sender != null);
                        this.sender.OnAttach(fields);
                    }
                    else
                    {
                        Fx.AssertAndThrow(1000, this.receiver != null);
                        this.receiver.OnAttach(fields);
                    }
                    break;
                }
                case 0x13:  // flow
                {
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
                            this.inWindow = window;
                            flow = new List() { this.nextIncomingId, this.inWindow, this.nextOutgoingId, this.outWindow };
                        }
                    }

                    if (flow != null)
                    {
                        this.transport.WriteFrame(0, 0, 0x13, flow);
                    }

                    if (this.receiver != null)
                    {
                        this.receiver.OnTransfer(fields, payload);
                    }
                    break;
                }
                case 0x15:  // disposition
                {
                    bool role = (bool)fields[0];
                    if (role)
                    {
                        this.sender.OnDisposition(fields);
                    }
                    break;
                }
                case 0x16:  // dettach
                {
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
                    this.state |= EndReceived;
                    break;
                case 0x18:  // close
                    this.state |= CloseReceived;
                    break;
                default:
                    Fx.AssertAndThrow(1000, false);
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

        static List Begin(uint nextOutgoingId, uint inWindow)
        {
            return new List() { null, nextOutgoingId, inWindow, 0u, 1u };
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
    }
}