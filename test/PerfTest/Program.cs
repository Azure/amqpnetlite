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
    using System.Reflection;
    using System.Threading;
    using Amqp;
    using Amqp.Framing;
    using Amqp.Listener;

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    throw new ArgumentException("args");
                }

                PerfArguments perfArgs = new PerfArguments(args);
                if (Arguments.IsHelp(perfArgs.Operation))
                {
                    Usage();
                    return;
                }

                if (perfArgs.TraceLevel != 0)
                {
                    Trace.TraceLevel = perfArgs.TraceLevel;
                    Trace.TraceListener = (f, o) => Console.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + string.Format(f, o));
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
            Console.WriteLine("  listen \tstart a listener and accept messages from remote peer");
            Console.WriteLine("\r\narguments:");
            typeof(PerfArguments).PrintArguments();
        }

        abstract class Role
        {
            PerfArguments perfArgs;
            int count;
            int started;
            int completed;
            int progress;
            ManualResetEvent completedEvent;
            System.Diagnostics.Stopwatch stopwatch;

            public Role(PerfArguments perfArgs)
            {
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

            protected bool OnStart()
            {
                return Interlocked.Increment(ref this.started) <= this.count || this.count == 0;
            }

            protected bool OnComplete()
            {
                int done = Interlocked.Increment(ref this.completed);
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
        }

        class Sender : Role
        {
            static OutcomeCallback onOutcome = OnSendComplete;
            int bodySize;
            SenderLink sender;

            public Sender(PerfArguments args)
                : base(args)
            {
                this.bodySize = args.BodySize;
            }

            public override void Run()
            {
                Connection connection = new Connection(new Address(this.Args.Address));
                Session session = new Session(connection);

                Attach attach = new Attach()
                {
                    Source = new Source(),
                    Target = new Target() { Address = this.Args.Node },
                    SndSettleMode = this.Args.SenderMode,
                    RcvSettleMode = this.Args.ReceiverMode
                };

                this.sender = new SenderLink(session, "perf-test-sender", attach, null);

                for (int i = 1; i <= this.Args.Queue; i++)
                {
                    if (this.OnStart())
                    {
                        Message message = new Message(new string('D', this.bodySize));
                        message.Properties = new Properties() { MessageId = "msg" };
                        sender.Send(message, onOutcome, this);
                    }
                }

                this.Wait();

                sender.Close();
                session.Close();
                connection.Close();
            }

            static void OnSendComplete(Message message, Outcome outcome, object state)
            {
                var thisPtr = (Sender)state;
                if (thisPtr.OnComplete())
                {
                    Message msg = new Message(new string('D', thisPtr.bodySize));
                    msg.Properties = new Properties() { MessageId = "msg" };
                    thisPtr.sender.Send(msg, onOutcome, state);
                }
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
                Connection connection = new Connection(new Address(this.Args.Address));
                Session session = new Session(connection);

                Attach attach = new Attach()
                {
                    Source = new Source() { Address = this.Args.Node },
                    Target = new Target(),
                    SndSettleMode = this.Args.SenderMode,
                    RcvSettleMode = this.Args.ReceiverMode
                };

                ReceiverLink receiver = new ReceiverLink(session, "perf-test-receiver", attach, null);
                receiver.Start(
                    this.Args.Queue,
                    (r, m) =>
                    {
                        r.Accept(m);
                        this.OnComplete();
                    });

                this.Wait();

                receiver.Close();
                session.Close();
                connection.Close();
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
                Uri addressUri = new Uri(this.Args.Address);
                ContainerHost host = new ContainerHost(new Uri[] { addressUri }, null, addressUri.UserInfo);
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

            [Argument(Name = "node", Shortcut = "n", Description = "name of the AMQP node", Default = "q1")]
            public string Node
            {
                get;
                protected set;
            }

            [Argument(Name = "count", Shortcut = "c", Description = "total number of messages to send or receive", Default = 100000)]
            public int Count
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

            [Argument(Name = "queue", Shortcut = "q", Description = "outgoing queue depth", Default = 1000)]
            public int Queue
            {
                get;
                protected set;
            }

            [Argument(Name = "progess", Shortcut = "p", Description = "report progess for every this number of messages")]
            public int Progress
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
                get { return this.Trace.TotraceLevel(); }
            }
        }
    }
}
