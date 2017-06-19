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

using System.Threading;
using Amqp;
using Amqp.Framing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Amqp
{
    [TestClass]
    public class PerfTests
    {
        TestTarget testTarget = new TestTarget();

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
        }

        int totalCount;
        int completedCount;
        int initialCount;
        int batchCount;
        OutcomeCallback onOutcome;
        SenderLink sender;
        ManualResetEvent done;

        [Description("Messages are sent unsettled. Completed count is incremented when the ack is received.")]
        //[TestMethod]
        public void PerfAtLeastOnceSend()
        {
            string testName = "PerfAtLeastOnceSend";
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);
            this.sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            this.onOutcome = OnSendComplete;
            this.done = new ManualResetEvent(false);
            this.totalCount = 1000000;
            this.completedCount = 0;
            this.initialCount = 300;
            this.batchCount = 100;
            Trace.TraceLevel = TraceLevel.Information;

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            this.SendMessages(initialCount);

            this.done.WaitOne();
            watch.Stop();
            Trace.WriteLine(TraceLevel.Information, "total: {0}, time: {1}ms", this.totalCount, watch.ElapsedMilliseconds);

            connection.Close();
        }

        void SendMessages(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                Message message = new Message("hello");
                message.Properties = new Properties() { MessageId = "msg" + i, GroupId = "perf" };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                this.sender.Send(message, onOutcome, this);
            }
        }

        static void OnSendComplete(ILink link, Message m, Outcome o, object state)
        {
            PerfTests thisPtr = (PerfTests)state;
            int sentCount = Interlocked.Increment(ref thisPtr.completedCount);
            if (sentCount >= thisPtr.totalCount)
            {
                thisPtr.done.Set();
            }
            else if (sentCount % thisPtr.batchCount == 0)
            {
                thisPtr.SendMessages(thisPtr.batchCount);
            }
        }
    }
}
