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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Test.Common;

namespace LongHaulTest
{
    class Consumer : Role<Consumer.ConsumeArguments>
    {
        public Consumer(string[] args, int start)
            : base(new ConsumeArguments(args, start))
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

            if (this.Args.UseMessageCallback)
            {
                tests.Add(new MyCallbackTest(this) { id = count++ });
            }

            return tests;
        }

        class MySyncTest : SyncTest<ReceiverLink>
        {
            public MySyncTest(Consumer consumer)
            {
                this.role = consumer;
            }

            protected override ReceiverLink CreateLink(Connection connection, Session session)
            {
                return new ReceiverLink(session, "Consumeer-link" + this.id, this.role.Args.Node);
            }

            protected override int Execute(ReceiverLink link)
            {
                Message message = link.Receive();
                if (message != null)
                {
                    link.Accept(message);
                    return 1;
                }

                return 0;
            }

            protected override int GetIterationDelay()
            {
                return 0;
            }
        }

        class MyAsyncTest : AsyncTest<ReceiverLink>
        {
            public MyAsyncTest(Consumer consumer)
            {
                this.role = consumer;
            }

            protected override ReceiverLink CreateLink(Connection connection, Session session)
            {
                return new ReceiverLink(session, "Consumeer-link" + this.id, this.role.Args.Node);
            }

            protected override async Task<int> ExecuteAsync(ReceiverLink link)
            {
                Message message = await link.ReceiveAsync();
                if (message != null)
                {
                    link.Accept(message);
                    return 1;
                }

                return 0;
            }

            protected override int GetIterationDelay()
            {
                return 0;
            }
        }

        class MyCallbackTest : SyncTest<ReceiverLink>
        {
            readonly AutoResetEvent reset;

            public MyCallbackTest(Consumer consumer)
            {
                this.role = consumer;
                this.reset = new AutoResetEvent(false);
            }

            void OnChannelClosed(IAmqpObject sender, Error error)
            {
                this.reset.Set();
            }

            protected override ReceiverLink CreateLink(Connection connection, Session session)
            {
                link = new ReceiverLink(session, "Consumeer-link" + this.id, this.role.Args.Node);
                this.connection.Closed += OnChannelClosed;
                this.session.Closed += OnChannelClosed;
                this.link.Closed += OnChannelClosed;
                this.link.Start(100, OnMessage);

                return link;
            }

            protected override int Execute(ReceiverLink link)
            {
                if (this.link.IsClosed || this.session.IsClosed || this.connection.IsClosed)
                {
                    throw new ObjectDisposedException("At least one amqp object is closed");
                }

                this.reset.WaitOne();
                return 0;
            }

            protected override int GetIterationDelay()
            {
                return 0;
            }

            void OnMessage(IReceiverLink link, Message message)
            {
                link.Accept(message);
                this.Success(1);
            }
        }

        public class ConsumeArguments : CommonArguments
        {
            public ConsumeArguments(string[] args, int start)
                : base(args, start)
            {
            }

            [Argument(Name = "callback", Shortcut = "o", Description = "use MessageCallback", Default = false)]
            public bool UseMessageCallback
            {
                get;
                protected set;
            }
        }
    }
}
