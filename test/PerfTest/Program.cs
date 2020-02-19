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

namespace PerfTest
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Amqp;
    using Amqp.Framing;
    using Amqp.Listener;
    using Amqp.Types;
    using Test.Common;
    using TestExtensions = Test.Common.Extensions;

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                PerfArguments perfArgs = new PerfArguments(args);
                if (args.Length == 0 || perfArgs.HasHelp)
                {
                    Usage();
                    return;
                }

                if (perfArgs.TraceLevel != 0)
                {
                    Trace.TraceLevel = perfArgs.TraceLevel;
                    Trace.TraceListener = (l, f, o) => Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, o));
                }

                Role role;
                if (string.Equals("send", perfArgs.Operation, StringComparison.OrdinalIgnoreCase))
                {
                    role = new Sender(perfArgs);
                }
                else if (string.Equals("receive", perfArgs.Operation, StringComparison.OrdinalIgnoreCase))
                {
                    role = new Receiver(perfArgs);
                }
                else if (string.Equals("request", perfArgs.Operation, StringComparison.OrdinalIgnoreCase))
                {
                    role = new Requestor(perfArgs);
                }
                else if (string.Equals("reply", perfArgs.Operation, StringComparison.OrdinalIgnoreCase))
                {
                    role = new ReplyListener(perfArgs);
                }
                else if (string.Equals("listen", perfArgs.Operation, StringComparison.OrdinalIgnoreCase))
                {
                    role = new Listener(perfArgs);
                }
                else
                {
                    throw new ArgumentException(perfArgs.Operation);
                }

                Console.WriteLine("Running perf test...");
                role.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void Usage()
        {
            Console.WriteLine(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe send|receive|listen [arguments]");
            Console.WriteLine("  send   \tsend messages to remote peer");
            Console.WriteLine("  receive\treceive messages from remote peer");
            Console.WriteLine("  request\tsend requests to a remote peer");
            Console.WriteLine("  reply  \tstart a request processor and send replies");
            Console.WriteLine("  listen \tstart a listener and accept messages from remote peer");
            Console.WriteLine("\r\narguments:");
            Arguments.PrintArguments(typeof(PerfArguments));
        }

        abstract class Role
        {
            protected IBufferManager bufferManager;
            PerfArguments perfArgs;
            long count;
            long started;
            long completed;
            long progress;
            ManualResetEvent completedEvent;
            System.Diagnostics.Stopwatch stopwatch;

            public Role(PerfArguments perfArgs)
            {
                this.bufferManager = perfArgs.BufferPooling ? new BufferManager(256, 2 * 1024 * 1024, 100 * 1024 * 1024) : null;
                this.perfArgs = perfArgs;
                this.count = perfArgs.Count;
                this.progress = perfArgs.Progress;
                this.completedEvent = new ManualResetEvent(false);
            }

            protected PerfArguments Args
            {
                 get { return this.perfArgs; }
            }

            public abstract void Run();

            protected Connection CreateConnection(Address address)
            {
                var factory = new ConnectionFactory();
                factory.BufferManager = this.bufferManager;
                factory.AMQP.MaxFrameSize = this.perfArgs.MaxFrameSize;
                if (address.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase))
                {
                    factory.SSL.RemoteCertificateValidationCallback = (a, b, c, d) => true;
                }

                return factory.CreateAsync(address).Result;
            }

            protected bool OnStart()
            {
                return Interlocked.Increment(ref this.started) <= this.count || this.count == 0;
            }

            protected bool OnComplete()
            {
                long done = Interlocked.Increment(ref this.completed);
                if (this.progress > 0 && done % this.progress == 0)
                {
                    long throughput;
                    if (this.stopwatch == null)
                    {
                        this.stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        throughput = -1;
                    }
                    else
                    {
                        long ms = this.stopwatch.ElapsedMilliseconds;
                        throughput = ms > 0 ? done * 1000L / ms : -1;
                    }

                    Trace.WriteLine(TraceLevel.Information, "completed {0} throughput {1} msg/s", done, throughput);
                }

                if (this.count > 0 && done >= this.count)
                {
                    this.stopwatch.Stop();
                    this.completedEvent.Set();
                    return false;
                }
                else
                {
                    return this.OnStart();
                }
            }

            protected void Wait()
            {
                this.completedEvent.WaitOne();
            }

            protected void SetComplete()
            {
                this.completedEvent.Set();
            }
        }

        class Sender : Role
        {
            static OutcomeCallback onOutcome = OnSendComplete;
            int bodySize;

            public Sender(PerfArguments args)
                : base(args)
            {
                this.bodySize = args.BodySize;
            }

            public override void Run()
            {
                Task[] tasks = new Task[this.Args.Connections];
                for (int i = 0; i < this.Args.Connections; i++)
                {
                    tasks[i] = Task.Run(() => this.RunOnce(i));
                }

                Task.WhenAll(tasks).Wait();
            }

            static void OnSendComplete(ILink link, Message message, Outcome outcome, object state)
            {
                var tuple = (Tuple<Sender, SenderLink>)state;
                Sender thisPtr = tuple.Item1;
                SenderLink sender = tuple.Item2;
                if (thisPtr.bufferManager != null)
                {
                    var buffer = message.GetBody<ByteBuffer>();
                    buffer.Reset();
                    thisPtr.bufferManager.ReturnBuffer(new ArraySegment<byte>(buffer.Buffer, buffer.Offset, buffer.Capacity));
                }

                if (thisPtr.OnComplete())
                {
                    Message msg = thisPtr.CreateMessage();
                    sender.Send(msg, onOutcome, state);
                }
            }

            void RunOnce(int id)
            {
                Connection connection = this.CreateConnection(new Address(this.Args.Address));
                connection.Closed += (o, e) => this.SetComplete();

                Session session = new Session(connection);

                Attach attach = new Attach()
                {
                    Source = new Source(),
                    Target = new Target() { Address = this.Args.Node },
                    SndSettleMode = this.Args.SenderMode,
                    RcvSettleMode = this.Args.ReceiverMode
                };

                SenderLink sender = new SenderLink(session, "perf-test-sender" + id, attach, null);

                for (int i = 1; i <= this.Args.Queue; i++)
                {
                    if (this.OnStart())
                    {
                        var message = this.CreateMessage();
                        sender.Send(message, onOutcome, Tuple.Create(this, sender));
                    }
                }

                this.Wait();

                sender.Close();
                session.Close();
                connection.Close();
            }

            Message CreateMessage()
            {
                ArraySegment<byte> segment = this.bufferManager != null ?
                    this.bufferManager.TakeBuffer(this.bodySize) :
                    new ArraySegment<byte>(new byte[this.bodySize]);
                int seed = DateTime.UtcNow.Millisecond;
                for (int i = 0; i < this.bodySize; i++)
                {
                    segment.Array[segment.Offset + i] = (byte)((i + seed) % 256);
                }

                Message message = new Message();
                message.Properties = new Properties() { MessageId = "msg" };
                message.BodySection = new Data()
                {
                    Buffer = new ByteBuffer(segment.Array, segment.Offset, this.bodySize, segment.Count)
                };

                return message;
            }
        }

        class Receiver : Role
        {
            public Receiver(PerfArguments args)
                : base(args)
            {
            }

            public override void Run()
            {
                Task[] tasks = new Task[this.Args.Connections];
                for (int i = 0; i < this.Args.Connections; i++)
                {
                    tasks[i] = Task.Run(() => this.RunOnce(i));
                }

                Task.WhenAll(tasks).Wait();
            }

            void RunOnce(int id)
            {
                Connection connection = this.CreateConnection(new Address(this.Args.Address));
                connection.Closed += (o, e) => this.SetComplete();

                Session session = new Session(connection);

                Attach attach = new Attach()
                {
                    Source = new Source() { Address = this.Args.Node },
                    Target = new Target(),
                    SndSettleMode = this.Args.SenderMode,
                    RcvSettleMode = this.Args.ReceiverMode
                };

                ReceiverLink receiver = new ReceiverLink(session, "perf-test-receiver" + id, attach, null);
                receiver.Start(
                    this.Args.Queue,
                    (r, m) =>
                    {
                        r.Accept(m);
                        m.Dispose();
                        this.OnComplete();
                    });

                this.Wait();

                receiver.Close();
                session.Close();
                connection.Close();
            }
        }

        class Requestor : Role
        {
            byte[] buffer;

            public Requestor(PerfArguments args)
                : base(args)
            {
                if (args.BufferPooling)
                {
                    this.buffer = new byte[args.BodySize];  // a simulation of buffer pooling
                }
            }

            public override void Run()
            {
                Task[] tasks = new Task[this.Args.Connections];
                for (int i = 0; i < this.Args.Connections; i++)
                {
                    tasks[i] = Task.Run(() => this.RunOnce(i));
                }

                Task.WhenAll(tasks).Wait();
            }

            void SendRequest(SenderLink sender, string replyTo)
            {
                Message message = new Message();
                message.Properties = new Properties() { ReplyTo = replyTo };
                message.Properties.SetCorrelationId(Guid.NewGuid());
                message.BodySection = new Data() { Binary = this.GetBuffer() };
                sender.Send(message, null, null);
            }

            byte[] GetBuffer()
            {
                return this.Args.BufferPooling ? this.buffer : new byte[this.Args.BodySize];
            }

            void RunOnce(int id)
            {
                Connection connection = this.CreateConnection(new Address(this.Args.Address));
                connection.Closed += (o, e) => this.SetComplete();
                Session session = new Session(connection);
                string clientId = "request-" + Guid.NewGuid().ToString().Substring(0, 6);
                Attach sendAttach = new Attach()
                {
                    Source = new Source(),
                    Target = new Target() { Address = this.Args.Node },
                    SndSettleMode = SenderSettleMode.Settled
                };
                Attach recvAttach = new Attach()
                {
                    Source = new Source() { Address = this.Args.Node },
                    Target = new Target() { Address = clientId },
                    SndSettleMode = SenderSettleMode.Settled
                };
                SenderLink sender = new SenderLink(session, "s-" + clientId, sendAttach, null);
                ReceiverLink receiver = new ReceiverLink(session, "r-" + clientId, recvAttach, null);
                receiver.Start(
                    50000,
                    (r, m) =>
                    {
                        r.Accept(m);
                        m.Dispose();
                        if (this.OnComplete())
                        {
                            this.SendRequest(sender, clientId);
                        }
                    });

                for (int i = 1; i <= this.Args.Queue; i++)
                {
                    if (this.OnStart())
                    {
                        this.SendRequest(sender, clientId);
                    }
                }

                this.Wait();

                connection.Close();
            }
        }

        class ReplyListener : Role, IRequestProcessor
        {
            public ReplyListener(PerfArguments args)
                : base(args)
            {
            }

            public override void Run()
            {
                Address addressUri = new Address(this.Args.Address);
                X509Certificate2 certificate = TestExtensions.GetCertificate(addressUri.Scheme, addressUri.Host, this.Args.CertValue);
                ContainerHost host = new ContainerHost(new Address[] { addressUri }, certificate);
                foreach (var listener in host.Listeners)
                {
                    listener.BufferManager = this.bufferManager;
                    listener.AMQP.MaxFrameSize = this.Args.MaxFrameSize;
                }

                host.Open();
                Console.WriteLine("Container host is listening on {0}:{1}", addressUri.Host, addressUri.Port);

                host.RegisterRequestProcessor(this.Args.Node, this);
                Console.WriteLine("Message processor is registered on {0}", this.Args.Node);

                this.Wait();

                host.Close();
            }

            int IRequestProcessor.Credit { get { return this.Args.Queue; } }

            void IRequestProcessor.Process(RequestContext requestContext)
            {
                Message response = new Message("request processed");
                response.ApplicationProperties = new ApplicationProperties();
                response.ApplicationProperties["status-code"] = 200;
                requestContext.Complete(response);
                this.OnComplete();
            }
        }

        class Listener : Role, IMessageProcessor
        {
            int credit;

            public Listener(PerfArguments args)
                : base(args)
            {
                this.credit = args.Queue;
            }

            public override void Run()
            {
                Address addressUri = new Address(this.Args.Address);
                X509Certificate2 certificate = TestExtensions.GetCertificate(addressUri.Scheme, addressUri.Host, this.Args.CertValue);
                ContainerHost host = new ContainerHost(new Address[] { addressUri }, certificate);
                foreach (var listener in host.Listeners)
                {
                    listener.BufferManager = this.bufferManager;
                    listener.AMQP.MaxFrameSize = this.Args.MaxFrameSize;
                }

                host.Open();
                Console.WriteLine("Container host is listening on {0}:{1}", addressUri.Host, addressUri.Port);

                host.RegisterMessageProcessor(this.Args.Node, this);
                Console.WriteLine("Message processor is registered on {0}", this.Args.Node);

                this.Wait();

                host.Close();
            }

            int IMessageProcessor.Credit
            {
                get { return this.credit; }
            }

            void IMessageProcessor.Process(MessageContext messageContext)
            {
                messageContext.Complete();
                this.OnComplete();
            }
        }

        class PerfArguments : Arguments
        {
            public PerfArguments(string[] args)
                : base(args, 1)
            {
                this.Operation = args[0];
            }

            public string Operation
            {
                get;
                private set;
            }

            [Argument(Name = "address", Shortcut = "a", Description = "address of the remote peer or the local listener", Default = "amqp://guest:guest@127.0.0.1:5672")]
            public string Address
            {
                get;
                protected set;
            }

            [Argument(Name = "cert", Shortcut = "f", Description = "certificate for SSL authentication. Default to address.host")]
            public string CertValue
            {
                get;
                protected set;
            }

            [Argument(Name = "node", Shortcut = "n", Description = "name of the AMQP node", Default = "q1")]
            public string Node
            {
                get;
                protected set;
            }

            [Argument(Name = "count", Shortcut = "c", Description = "total number of messages to send or receive (0: infinite)", Default = 100000)]
            public long Count
            {
                get;
                protected set;
            }

            [Argument(Name = "connection", Shortcut = "i", Description = "number of connection to create", Default = 1)]
            public int Connections
            {
                get;
                protected set;
            }

            [Argument(Name = "body-size", Shortcut = "b", Description = "message body size (bytes)", Default = 64)]
            public int BodySize
            {
                get;
                protected set;
            }

            [Argument(Name = "max-frame-size", Shortcut = "m", Description = "connection max frame size (bytes)", Default = 256*1024)]
            public int MaxFrameSize
            {
                get;
                protected set;
            }

            [Argument(Name = "queue", Shortcut = "q", Description = "outgoing queue depth (link credit)", Default = 1000)]
            public int Queue
            {
                get;
                protected set;
            }

            [Argument(Name = "progress", Shortcut = "p", Description = "report progress for every this number of messages", Default = 1000)]
            public int Progress
            {
                get;
                protected set;
            }

            [Argument(Name = "buffer-pool", Shortcut = "u", Description = "enable buffer pooling", Default = false)]
            public bool BufferPooling
            {
                get;
                protected set;
            }

            [Argument(Name = "ack", Shortcut = "k", Description = "ack mode: amo|alo|eo", Default = "amo")]
            protected string Ack
            {
                get;
                set;
            }

            [Argument(Name = "trace", Shortcut = "t", Description = "trace level: err|warn|info|verbose|frm", Default = "info")]
            protected string Trace
            {
                get;
                set;
            }

            public SenderSettleMode SenderMode
            {
                get { return this.Ack.ToSenderSettleMode(); }
            }

            public ReceiverSettleMode ReceiverMode
            {
                get { return this.Ack.ToReceiverSettleMode(); }
            }

            public TraceLevel TraceLevel
            {
                get { return this.Trace.ToTraceLevel(); }
            }
        }
    }
}
