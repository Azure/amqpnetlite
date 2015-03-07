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
    using System.Threading;
    using Amqp.Framing;

    public delegate void ClosedCallback(AmqpObject sender, Error error);

    public abstract class AmqpObject
    {
        const int DefaultCloseTimeout = 60000;
        ManualResetEvent endEvent;

        public ClosedCallback Closed
        {
            get;
            set;
        }

        protected void NotifyClosed(Error error)
        {
            ManualResetEvent temp = this.endEvent;
            if (temp != null)
            {
                temp.Set();
            }

            ClosedCallback closed = this.Closed;
            if (closed != null)
            {
                closed(this, error);
            }
        }

        public void Close(int waitUntilEnded = DefaultCloseTimeout, Error error = null)
        {
            // initialize event first to avoid the race with NotifyClosed
            this.endEvent = new ManualResetEvent(false);
            if (!this.OnClose(error))
            {
                if (waitUntilEnded > 0)
                {
                    this.endEvent.WaitOne(waitUntilEnded, false);
                }
            }
            else
            {
                this.endEvent.Set();
                this.NotifyClosed(null);
            }
        }

        protected abstract bool OnClose(Error error);
    }
}