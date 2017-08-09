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
    }

    public enum TestOutcome
    {
        Continue,
        Stop,
    }

    public delegate TestOutcome TestFunc(Stream stream, ushort channel, List fields);

    public class TestListener
    {
        readonly IPEndPoint ip;
        readonly Dictionary<TestPoint, TestFunc> testPoints;
        Socket socket;
        SocketAsyncEventArgs args;

        public TestListener(IPEndPoint ip)
        {
            this.ip = ip;
            this.testPoints = new Dictionary<TestPoint, TestFunc>();
        }

        public static void FRM(Stream stream, ulong code, byte type, ushort channel, params object[] value)
        {
            List list = new List();
            if (value != null) list.AddRange(value);
            ByteBuffer buffer = new ByteBuffer(256, true);
            buffer.Append(4);
            AmqpBitConverter.WriteUByte(buffer, 2);
            AmqpBitConverter.WriteUByte(buffer, type);
            AmqpBitConverter.WriteUShort(buffer, channel);
            Encoder.WriteObject(buffer, new DescribedValue(code, list));
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
                    FRM(stream, 0x40UL, 1, 0, new Symbol[] { "PLAIN", "EXTERNAL", "ANONYMOUS" });
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
            buffer.Complete(1);
            ulong code = Encoder.ReadULong(buffer, Encoder.ReadFormatCode(buffer));
            List fields = Encoder.ReadList(buffer, Encoder.ReadFormatCode(buffer));
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
                        FRM(stream, 0x11UL, 0, channel, channel, 0u, 100u, 100u, 8u);
                    }
                    break;

                case 0x12ul:  // attach
                    if (this.HandleTestPoint(TestPoint.Attach, stream, channel, fields) == TestOutcome.Continue)
                    {
                        bool role = !(bool)fields[2];
                        FRM(stream, 0x12UL, 0, channel, fields[0], fields[1], role, fields[3], fields[4], new Source(), new Target());
                        if (role)
                        {
                            FRM(stream, 0x13UL, 0, channel, 0u, 100u, 0u, 100u, fields[1], 0u, 1000u);
                        }
                    }
                    break;

                case 0x13ul:  // flow
                    if (this.HandleTestPoint(TestPoint.Flow, stream, channel, fields) == TestOutcome.Continue)
                    {
                    }
                    break;

                case 0x14ul:  // transfer
                    if (this.HandleTestPoint(TestPoint.Transfer, stream, channel, fields) == TestOutcome.Continue)
                    {
                        FRM(stream, 0x15UL, 0, channel, true, fields[1], null, true, new Accepted());
                    }
                    break;

                case 0x15ul:  // disposition
                    if (this.HandleTestPoint(TestPoint.Disposition, stream, channel, fields) == TestOutcome.Continue)
                    {
                    }
                    break;

                case 0x16ul:  // dettach
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
                        if (!OnFrame(stream, new ByteBuffer(frame, 0, frame.Length, frame.Length))) break;
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
