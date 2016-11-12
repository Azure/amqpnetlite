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
    interface IRunnable
    {
        void Run();
        void Dispose();
        void Report();
    }

    abstract class Role<TArgs> : IRunnable where TArgs : CommonArguments
    {
        Timer timer;
        IList<IRunnable> tests;
        long success;
        long failure;

        protected Role(TArgs args)
        {
            this.Args = args;
        }

        public TArgs Args
        {
            get;
            private set;
        }

        public Address Address
        {
            get { return new Address(this.Args.Address); }
        }

        public void Run()
        {
            SetTrace(this.Args);

            this.timer = new Timer(OnTimer, this, this.Args.Progress * 1000, this.Args.Progress * 1000);

            this.tests = this.CreateTests();
            for (int i = 0; i < tests.Count; i++)
            {
                Task.Factory.StartNew(r => ((IRunnable)r).Run(), tests[i]);
            }

            Thread.Sleep(this.Args.Duration == 0 ? -1 : this.Args.Duration * 1000);

            this.timer.Dispose();
            for (int i = 0; i < tests.Count; i++)
            {
                this.tests[i].Dispose();
                this.tests[i].Report();
            }

            this.Report();
        }

        public void Dispose()
        {
        }

        public void Report()
        {
            Console.WriteLine("{0,-20}Success\t{1}\tFailure\t{2}", this.GetType().Name, this.success, this.failure);
        }

        public void Progress(long s, long f)
        {
            Interlocked.Add(ref this.success, s);
            Interlocked.Add(ref this.failure, f);
        }

        public abstract IList<IRunnable> CreateTests();

        static void OnTimer(object state)
        {
            Role<TArgs> thisPtr = (Role<TArgs>)state;
            for (int i = 0; i < thisPtr.tests.Count; i++)
            {
                thisPtr.tests[i].Report();
            }

            thisPtr.Report();
        }

        static void SetTrace(CommonArguments args)
        {
            if (Trace.TraceListener == null)
            {
                Trace.TraceLevel = args.Trace.ToTraceLevel();
                Trace.TraceListener = (f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));
            }
        }

        public abstract class Test<TLink> : IRunnable where TLink : Link
        {
            public int id;
            protected bool disposed;
            protected Role<TArgs> role;
            protected long total;
            protected Random random = new Random();
            protected Connection connection;
            protected Session session;
            protected TLink link;
            long success;
            long failure;
            long iterationSuccess;
            long iterationFailure;

            public abstract void Run();

            protected abstract int GetIterationDelay();

            protected abstract TLink CreateLink(Connection connection, Session session);

            public void Dispose()
            {
                this.disposed = true;
            }

            public void Report()
            {
                long its = Interlocked.Exchange(ref iterationSuccess, 0);
                long itf = Interlocked.Exchange(ref iterationFailure, 0);
                long ts = Interlocked.Add(ref this.success, its);
                long tf = Interlocked.Add(ref this.failure, itf);

                Console.WriteLine("{0,-20}Success\t{1}\tFailure\t{2}\tIteration\t{3}\t{4}",
                    this.GetType().Name + this.id, ts, tf, its, itf);
            }

            protected void Success(int count)
            {
                Interlocked.Add(ref this.iterationSuccess, count);
                this.role.Progress(count, 0);
            }

            protected void Failure(int count)
            {
                Interlocked.Add(ref this.iterationFailure, count);
                this.role.Progress(0, count);
            }

            protected void EnsureConnection()
            {
                if (this.connection == null || this.connection.IsClosed)
                {
                    Address address = this.role.Address;
                    Connection.DisableServerCertValidation = true;
                    this.connection = new Connection(
                        address,
                        null,
                        new Open()
                        {
                            ContainerId = "amqp-test" + this.id,
                            HostName = this.role.Args.Host ?? address.Host
                        },
                        null);
                }
            }

            protected async Task EnsureConnectionAsync()
            {
                if (this.connection == null || this.connection.IsClosed)
                {
                    Address address = this.role.Address;
                    ConnectionFactory factory = new ConnectionFactory();
                    factory.SSL.RemoteCertificateValidationCallback = (a, b, c, e) => true;
                    factory.AMQP.HostName = this.role.Args.Host ?? address.Host;
                    factory.AMQP.ContainerId = "amqp-test" + this.id;
                    this.connection = await factory.CreateAsync(address);
                }
            }

            protected void EnsureLink()
            {
                if (this.link == null || this.link.IsClosed)
                {
                    if (this.session != null)
                    {
                        this.session.Close(0);
                    }

                    this.session = new Session(connection);
                    this.link = this.CreateLink(this.connection, this.session);
                }
            }
        }

        public abstract class SyncTest<TLink> : Test<TLink> where TLink : Link
        {
            public override void Run()
            {
                while (!this.disposed)
                {
                    int delay = 0;
                    try
                    {
                        this.EnsureConnection();
                        this.EnsureLink();
                        int result = this.Execute(this.link);
                        this.Success(result);
                        delay = this.GetIterationDelay();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.ToString());
                        this.Failure(1);
                        delay = 1000;
                    }

                    if (delay > 0)
                    {
                        Thread.Sleep(delay);
                    }
                }

                if (this.connection != null)
                {
                    try
                    {
                        this.connection.Close();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.ToString());
                    }
                }
            }

            protected abstract int Execute(TLink link);
        }

        public abstract class AsyncTest<TLink> : Test<TLink> where TLink : Link
        {
            public async override void Run()
            {
                while (!this.disposed)
                {
                    int delay = 0;
                    try
                    {
                        await this.EnsureConnectionAsync();
                        this.EnsureLink();
                        int result = await this.ExecuteAsync(this.link);
                        this.Success(result);
                        delay = this.GetIterationDelay();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.ToString());
                        this.Failure(1);
                        delay = 1000;
                    }

                    if (delay > 0)
                    {
                        await Task.Delay(delay);
                    }
                }

                if (this.connection != null)
                {
                    try
                    {
                        await this.connection.CloseAsync();
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.ToString());
                    }
                }
            }

            protected abstract Task<int> ExecuteAsync(TLink link);
        }
    }
}
