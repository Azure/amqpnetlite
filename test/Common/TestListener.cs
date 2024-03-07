//  ------------------------------------------------------------------------------------
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Amqp;
using Amqp.Framing;
using Amqp.Types;

namespace Test.Amqp
{
    public enum TestPoint
    {
        None = 0,

        SaslHeader,
        SaslInit,
        SaslMechamisms,

        Header,
        Open,
        Begin,
        Attach,
        Flow,
        Transfer,
        Disposition,
        Detach,
        End,
        Close,

        Empty,
    }

    public enum TestOutcome
    {
        Continue,
        Stop,
    }

    public delegate TestOutcome TestFunc(Stream stream, ushort channel, List fields);

    public class TestListener
    {
        readonly X509Certificate2 cert;
        readonly IPEndPoint ip;
        readonly Dictionary<TestPoint, TestFunc> testPoints;
        Socket socket;
        SocketAsyncEventArgs args;

        public uint WindowSize { get; set; }

        public uint LinkCredit { get; set; }

        public TestListener(IPEndPoint ip)
            : this(ip, null)
        {
        }

        public TestListener(IPEndPoint ip, string sslHost)
        {
            this.WindowSize = 5000u;
            this.LinkCredit = 300u;
            this.ip = ip;
            this.testPoints = new Dictionary<TestPoint, TestFunc>();
            if (sslHost != null)
            {
                this.cert = Test.Common.Extensions.GetCertificate("amqps", sslHost, null);
            }
        }

        public static void FRM(Stream stream, ulong code, byte type, ushort channel, params object[] value)
        {
            ArraySegment<byte> payload = default(ArraySegment<byte>);
            if (code == 0x14UL) // transfer
            {
                payload = new ArraySegment<byte>(new byte[] { 0x00, 0x53, 0x77, 0xa1, 0x05, 0x48, 0x65, 0x6c, 0x6c, 0x6f });
            }
            FRM(stream, code, type, channel, payload, value);
        }

        public static void FRM(Stream stream, ulong code, byte type, ushort channel, ArraySegment<byte> payload, params object[] value)
        {
            List list = new List();
            if (value != null) list.AddRange(value);
            ByteBuffer buffer = new ByteBuffer(256, true);
            buffer.Append(4);
            AmqpBitConverter.WriteUByte(buffer, 2);
            AmqpBitConverter.WriteUByte(buffer, type);
            AmqpBitConverter.WriteUShort(buffer, channel);
            Encoder.WriteObject(buffer, new DescribedValue(code, list));
            if (payload.Count > 0)
            {
                AmqpBitConverter.WriteBytes(buffer, payload.Array, payload.Offset, payload.Count);
            }

            AmqpBitConverter.WriteInt(buffer.Buffer, 0, buffer.Length);
            stream.Write(buffer.Buffer, buffer.Offset, buffer.Length);
        }

        public void Open()
        {
            this.args = new SocketAsyncEventArgs();
            this.args.Completed += this.OnAccept;

            this.socket = new Socket(this.ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            this.socket.Bind(this.ip);
            this.socket.Listen(20);
            this.Accept();
        }

        public void Close()
        {
            Socket s = this.socket;
            this.socket = null;
            if (s != null) s.Dispose();
            if (this.args != null) this.args.Dispose();
        }

        public void RegisterTarget(TestPoint point, TestFunc func)
        {
            this.testPoints[point] = func;
        }

        public void ResetTargets()
        {
            this.testPoints.Clear();
        }

        static void Read(Stream stream, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int bytes = stream.Read(buffer, offset, count);
                if (bytes == 0)
                {
                    throw new ObjectDisposedException("socket");
                }

                offset += bytes;
                count -= bytes;
            }
        }

        void Accept()
        {
            Socket s = this.socket;
            while (s != null)
            {
                try
                {
                    if (this.socket.AcceptAsync(this.args))
                    {
                        break;
                    }

                    this.args.UserToken = "sync";
                    this.OnAccept(s, this.args);
                }
                catch { }

                s = this.socket;
            }
        }

        void OnAccept(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                Socket s = args.AcceptSocket;
                s.NoDelay = true;
                System.Threading.Tasks.Task.Factory.StartNew(() => this.Pump(new NetworkStream(s, true)));
            }

            bool sync = args.UserToken != null;
            args.UserToken = null;
            args.AcceptSocket = null;
            if (!sync)
            {
                this.Accept();
            }
        }

        TestOutcome HandleTestPoint(TestPoint point, Stream stream, ushort channel, List fields)
        {
            TestFunc func;
            if (this.testPoints.TryGetValue(point, out func))
            {
                return func(stream, channel, fields);
            }

            return TestOutcome.Continue;
        }

        void OnHeader(Stream stream, byte[] buffer)
        {
            if (buffer[4] == 3)
            {
                if (this.HandleTestPoint(TestPoint.SaslHeader, stream, 0, null) == TestOutcome.Continue)
                {
                    stream.Write(buffer, 0, buffer.Length);
                }
                if (this.HandleTestPoint(TestPoint.SaslMechamisms, stream, 0, null) == TestOutcome.Continue)
                {
                    var mechanisms = new Symbol[] { "PLAIN", "EXTERNAL", "ANONYMOUS" };
                    FRM(stream, 0x40UL, 1, 0, new object[] { mechanisms });
                }
            }
            else
            {
                if (this.HandleTestPoint(TestPoint.Header, stream, 0, null) == TestOutcome.Continue)
                {
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }

        bool OnFrame(Stream stream, ByteBuffer buffer)
        {
            buffer.Complete(1);
            byte type = AmqpBitConverter.ReadUByte(buffer);
            ushort channel = AmqpBitConverter.ReadUShort(buffer);
            if (buffer.Length == 0)
            {
                this.HandleTestPoint(TestPoint.Empty, stream, channel, null);
                return true;
            }

            buffer.Complete(1);
            ulong code = Encoder.ReadULong(buffer, Encoder.ReadFormatCode(buffer));
            List fields = Encoder.ReadList(buffer, Encoder.ReadFormatCode(buffer));
            if (buffer.Length > 0)
            {
                fields.Add(buffer);
            }

            switch (code)
            {
                case 0x41ul:  // sasl-init
                    if (this.HandleTestPoint(TestPoint.SaslInit, stream, channel, fields) == TestOutcome.Continue)
                    {
                        FRM(stream, 0x44ul, 1, 0, (byte)0);
                    }
                    return false;

                case 0x10ul:  // open
                    if (this.HandleTestPoint(TestPoint.Open, stream, channel, fields) == TestOutcome.Continue)
                    {
                        FRM(stream, 0x10UL, 0, 0, "TestListener");
                    }
                    break;

                case 0x11ul:  // begin
                    if (this.HandleTestPoint(TestPoint.Begin, stream, channel, fields) == TestOutcome.Continue)
                    {
                        FRM(stream, 0x11UL, 0, channel, channel, 0u, WindowSize, WindowSize, 256u);
                    }
                    break;

                case 0x12ul:  // attach
                    if (this.HandleTestPoint(TestPoint.Attach, stream, channel, fields) == TestOutcome.Continue)
                    {
                        bool role = !(bool)fields[2];
                        FRM(stream, 0x12UL, 0, channel, fields[0], fields[1], role, fields[3], fields[4], fields[5], fields[6], null, null, 0u, 4000000ul);
                        if (role)
                        {
                            FRM(stream, 0x13UL, 0, channel, 0u, WindowSize, 0u, WindowSize, fields[1], 0u, LinkCredit);
                        }
                    }
                    break;

                case 0x13ul:  // flow
                    if (this.HandleTestPoint(TestPoint.Flow, stream, channel, fields) == TestOutcome.Continue)
                    {
                        if (fields[4] != null)
                        {
                            uint deliveryCount = (uint)fields[5];
                            uint credit = (uint)fields[6];
                            for (uint i = 0; i < credit; i++)
                            {
                                var msg = new Message();
                                msg.Properties = new Properties { MessageId = Guid.NewGuid().ToString() };
                                msg.MessageAnnotations = new MessageAnnotations();
                                msg.MessageAnnotations.Map["x-opt-enqueued-time"] = DateTime.UtcNow;
                                msg.MessageAnnotations.Map["x-opt-sequence-number"] = (long)deliveryCount;
                                msg.BodySection = new AmqpValue() { Value = "test message " + deliveryCount };
                                var b = msg.Encode();
                                TestListener.FRM(stream, 0x14UL, 0, channel, new ArraySegment<byte>(b.Buffer, b.Offset, b.Length),
                                    fields[4], deliveryCount++, Guid.NewGuid().ToByteArray(), 0u, false, false);  // transfer
                            }
                        }
                    }
                    break;

                case 0x14ul:  // transfer
                    if (this.HandleTestPoint(TestPoint.Transfer, stream, channel, fields) == TestOutcome.Continue)
                    {
                        if (fields[4] == null || false.Equals(fields[4]))
                        {
                            FRM(stream, 0x15UL, 0, channel, true, fields[1], null, true, new Accepted());
                        }

                        uint deliveryId = (uint)fields[1] + 1u;
                        if (deliveryId % 200u == 0)
                        {
                            TestListener.FRM(stream, 0x13UL, 0, channel, deliveryId, WindowSize, 0u, WindowSize, fields[0], deliveryId, LinkCredit);
                        }
                    }
                    break;

                case 0x15ul:  // disposition
                    if (this.HandleTestPoint(TestPoint.Disposition, stream, channel, fields) == TestOutcome.Continue)
                    {
                        if (fields[3] == null || false.Equals(fields[4]))
                        {
                            FRM(stream, 0x15UL, 0, channel, !(bool)fields[0], fields[1], fields[2], true, fields[4]);
                        }
                    }
                    break;

                case 0x16ul:  // detach
                    if (this.HandleTestPoint(TestPoint.Detach, stream, channel, fields) == TestOutcome.Continue)
                    {
                        FRM(stream, 0x16UL, 0, channel, fields[0], true);
                    }
                    break;

                case 0x17ul:  // end
                    if (this.HandleTestPoint(TestPoint.End, stream, channel, fields) == TestOutcome.Continue)
                    {
                        FRM(stream, 0x17UL, 0, channel);
                    }
                    break;

                case 0x18ul:  // close
                    if (this.HandleTestPoint(TestPoint.Close, stream, channel, fields) == TestOutcome.Continue)
                    {
                        FRM(stream, 0x18UL, 0, channel);
                    }
                    return false;

                default:
                    break;
            }

            return true;
        }

        void Pump(Stream stream)
        {
            try
            {
                if (this.cert != null)
                {
                    var ssl = new System.Net.Security.SslStream(stream, false);
                    ssl.AuthenticateAsServer(cert);
                    stream = ssl;
                }

                while (true)
                {
                    byte[] buffer = new byte[8];
                    Read(stream, buffer, 0, 8);
                    OnHeader(stream, buffer);

                    while (true)
                    {
                        Read(stream, buffer, 0, 4);
                        int len = AmqpBitConverter.ReadInt(buffer, 0);
                        byte[] frame = new byte[len - 4];
                        Read(stream, frame, 0, frame.Length);
                        if (!OnFrame(stream, new ByteBuffer(frame, 0, frame.Length, frame.Length)))
                        {
                            break;
                        }
                    }
                }
            }
            catch
            {
                stream.Dispose();
            }
        }
    }
}
