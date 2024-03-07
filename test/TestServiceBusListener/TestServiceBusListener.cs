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

namespace Test.ServiceBusListener
{
    using System;
    using System.Net;
    using global::Amqp;
    using global::Amqp.Framing;
    using Test.Amqp;

    public class TestServiceBusListener : TestListener
    {
        ulong cbsSender = ulong.MaxValue;
        ulong cbsReceiver = ulong.MaxValue;
        uint inTotal = 0;
        uint outTotal = 0;

        public TestServiceBusListener(Uri address)
            : base(GetEndPoint(address, out string sslHost), sslHost)
        {
            this.Init();
        }

        static IPEndPoint GetEndPoint(Uri address, out string sslHost)
        {
            sslHost = null;
            if (string.Equals(address.Scheme, "amqps", StringComparison.OrdinalIgnoreCase))
            {
                sslHost = address.Host;
            }

            int port = address.Port > 0 ? address.Port : (sslHost != null ? 5671 : 5672);
            var list = Dns.GetHostAddresses(address.Host);
            return new IPEndPoint(list[0], port);
        }

        static ulong Client(ushort channel, uint handle)
        {
            return (ulong)channel << 32 | handle;
        }

        void Init()
        {
            this.RegisterTarget(TestPoint.Open, (stream, channel, fields) =>
            {
                inTotal = 0;
                outTotal = 0;
                return TestOutcome.Continue;
            });

            this.RegisterTarget(TestPoint.Attach, (stream, channel, fields) =>
            {
                if ((bool)fields[2])
                {
                    if (string.Equals("$cbs", ((Source)fields[5]).Address, StringComparison.OrdinalIgnoreCase))
                    {
                        cbsReceiver = Client(channel, (uint)fields[1]);
                    }
                }
                else
                {
                    if (string.Equals("$cbs", ((Target)fields[6]).Address, StringComparison.OrdinalIgnoreCase))
                    {
                        cbsSender = Client(channel, (uint)fields[1]);
                    }
                }

                return TestOutcome.Continue;
            });

            this.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                if (fields[4] != null && cbsReceiver != Client(channel, (uint)fields[4]))
                {
                    uint limit = (uint)fields[5] + (uint)fields[6];
                    while (outTotal < limit)
                    {
                        var msg = new Message();
                        msg.MessageAnnotations = new MessageAnnotations();
                        msg.MessageAnnotations.Map["x-opt-enqueued-time"] = DateTime.UtcNow;
                        msg.MessageAnnotations.Map["x-opt-sequence-number"] = (long)outTotal;
                        msg.MessageAnnotations.Map["x-opt-locked-until"] = DateTime.UtcNow + TimeSpan.FromDays(1);
                        msg.BodySection = new AmqpValue() { Value = "test" };
                        var b = msg.Encode();
                        TestListener.FRM(stream, 0x14UL, 0, channel, new ArraySegment<byte>(b.Buffer, b.Offset, b.Length),
                            fields[4], outTotal, Guid.NewGuid().ToByteArray(), 0u, false, false);  // transfer
                        outTotal++;
                    }
                }

                return TestOutcome.Stop;
            });

            this.RegisterTarget(TestPoint.Transfer, (stream, channel, fields) =>
            {
                if (cbsSender == Client(channel, (uint)fields[0]))
                {
                    var mb = fields[fields.Count - 1] as ByteBuffer;
                    if (mb != null)
                    {
                        var id = Message.Decode(mb).Properties.MessageId;
                        var p = new ApplicationProperties();
                        p.Map["status-code"] = 202;
                        p.Map["status-description"] = "Accepted";
                        var reply = new Message() { Properties = new Properties { CorrelationId = id }, ApplicationProperties = p };
                        var b = reply.Encode();
                        var payload = new ArraySegment<byte>(b.Buffer, b.Offset, b.Length);
                        TestListener.FRM(stream, 0x14UL, 0, (ushort)(cbsReceiver >> 32), payload, (uint)cbsReceiver, fields[1], fields[2], fields[3], true, false);
                    }
                }
                else
                {
                    inTotal++;
                    if (inTotal % 200u == 0)
                    {
                        TestListener.FRM(stream, 0x13UL, 0, channel, inTotal, 5000u, 0u, 5000u, fields[0], inTotal, 300u);
                    }

                    if (inTotal > 100000)
                    {
                        stream.Dispose();
                    }
                }

                if (fields[4] == null || false.Equals(fields[4]))
                {
                    TestListener.FRM(stream, 0x15UL, 0, channel, true, fields[1], null, true, new Accepted());
                }

                return TestOutcome.Stop;
            });

            this.RegisterTarget(TestPoint.Disposition, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x15UL, 0, channel, !(bool)fields[0], fields[1], fields[2], true, fields[4]);
                return TestOutcome.Stop;
            });
        }
    }
}
