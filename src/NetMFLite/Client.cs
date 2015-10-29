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

        internal const string Name = "netmf-lite";
        internal const int MaxFrameSize = 16 * 1024;
        string host;
        int port;
        bool useSsl;
        string userName;
        string password;

        internal AutoResetEvent signal;
        internal byte state;
        internal NetworkStream transport;
        internal int maxFrameSize = MaxFrameSize;

        Sender sender;
        uint outWindow = 50;

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
                Fx.AssertAndThrow(1000, this.signal.WaitOne(millisecondsTimeout, true));
            }
        }

        internal void Send(object message, uint deliveryId, bool settled)
        {
            DescribedValue value = new DescribedValue(0x77ul, message);
            ByteBuffer buffer = new ByteBuffer(128, true);
            buffer.AdjustPosition(Extensions.TransferFramePrefixSize, 0);   // reserve space for frame header and transfer
            Encoder.WriteObject(buffer, value, true);

            while (buffer.Length > 0)
            {
                this.Wait(o => ((Client)o).outWindow == 0, this, 60000);
                lock (this)
                {
                    this.outWindow--;
                }
                this.transport.WriteTransferFrame(deliveryId, settled, buffer, this.maxFrameSize);
            }
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
            this.transport.WriteFrame(0, 0, 0x11, Begin());

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
            while (this.state > 0)
            {
                try
                {
                    this.transport.ReadFrame(out frameType, out channel, out code, out fields, out payload);
                    this.OnFrame(code, fields, payload);
                    this.signal.Set();
                }
                catch (Exception)
                {
                    this.transport.Close();
                    this.state = 0;
                }
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
                    this.state |= BeginReceived;
                    break;
                case 0x12:  // attach
                {
                    bool role = (bool)fields[2];
                    if (!role)
                    {
                        Fx.AssertAndThrow(1000, this.sender != null);
                        this.sender.OnAttach(fields);
                    }

                    break;
                }
                case 0x13:  // flow
                {
                    lock(this)
                    {

                    }
                    if (fields[4] != null)
                    {
                        uint handle = (uint)fields[4];
                        Fx.AssertAndThrow(1000, handle < 2u);
                        if (handle == 0u)
                        {
                            this.sender.OnFlow(fields);
                        }
                    }
                    break;
                }
                case 0x14:  // transfer
                    break;
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
                    if (handle == 0)
                    {
                        this.sender.OnDetach(fields);
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

        static List Begin()
        {
            return new List() { null, 0u, 100u, 100u, 0u };
        }
    }
}