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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Test.Common;

namespace LongHaulTest
{
    class Publisher : Role<Publisher.PublishArguments>
    {
        public Publisher(string[] args, int start)
            : base(new PublishArguments(args, start))
        {
        }

        public override IList<IRunnable> CreateTests()
        {
            List<IRunnable> tests = new List<IRunnable>();
            int count = 1;
            if (this.Args.Synchronous)
            {
                tests.Add(new MySyncTest(this) { id = count++ });
            }

            if (this.Args.Asynchronous)
            {
                tests.Add(new MyAsyncTest(this) { id = count++ });
            }

            return tests;
        }

        static Message[] CreateMessages(int testId, long start, int count)
        {
            Message[] messages = new Message[count];
            for (int i = 0; i < count; i++)
            {
                messages[i] = new Message("test")
                {
                    Properties = new Properties()
                    {
                        MessageId = string.Format("message-{0}-{1}", testId, start++)
                    }
                };
            }

            return messages;
        }

        class MySyncTest : SyncTest<SenderLink>
        {
            AutoResetEvent done = new AutoResetEvent(false);
            int remaining;

            public MySyncTest(Publisher publisher)
            {
                this.role = publisher;
            }

            protected override SenderLink CreateLink(Connection connection, Session session)
            {
                return new SenderLink(session, "publisher-link" + this.id, this.role.Args.Node);
            }

            protected override int Execute(SenderLink link)
            {
                this.remaining = this.random.Next(1, this.role.Args.Batch);
                Message[] messages = CreateMessages(this.id, this.total, this.remaining);
                for (int i = 0; i < messages.Length; i++)
                {
                    link.Send(
                        messages[i],
                        (l, m, o, s) =>
                        {
                            MySyncTest thisPtr = (MySyncTest)s;
                            if (o is Accepted)
                            {
                                thisPtr.Success(1);
                            }
                            else
                            {
                                thisPtr.Failure(1);
                            }

                            if (Interlocked.Decrement(ref thisPtr.remaining) <= 0)
                            {
                                thisPtr.done.Set();
                            }
                        },
                        this);
                }

                this.done.WaitOne();

                this.total += messages.Length;
                return 0;
            }

            protected override int GetIterationDelay()
            {
                return this.random.Next(this.role.Args.MinDelay, this.role.Args.MaxDelay);
            }
        }

        class MyAsyncTest : AsyncTest<SenderLink>
        {
            public MyAsyncTest(Publisher publisher)
            {
                this.role = publisher;
            }

            protected override SenderLink CreateLink(Connection connection, Session session)
            {
                return new SenderLink(session, "publisher-link" + this.id, this.role.Args.Node);
            }

            protected override async Task<int> ExecuteAsync(SenderLink link)
            {
                int batch = this.random.Next(1, this.role.Args.Batch);
                Message[] messages = CreateMessages(this.id, this.total, batch);
                await Task.WhenAll(messages.Select(m => link.SendAsync(m)));
                this.total += batch;
                return batch;
            }

            protected override int GetIterationDelay()
            {
                return this.random.Next(this.role.Args.MinDelay, this.role.Args.MaxDelay);
            }
        }
        
        public class PublishArguments : CommonArguments
        {
            public PublishArguments(string[] args, int start)
                : base(args, start)
            {
            }

            [Argument(Name = "batch", Shortcut = "b", Description = "max number of messages to send in one iteration", Default = 10)]
            public int Batch
            {
                get;
                protected set;
            }

            [Argument(Name = "min-delay", Shortcut = "i", Description = "min delay to next send in ms", Default = 10)]
            public int MinDelay
            {
                get;
                protected set;
            }

            [Argument(Name = "max-delay", Shortcut = "x", Description = "max delay to next send in ms", Default = 1000)]
            public int MaxDelay
            {
                get;
                protected set;
            }
        }
    }
}
