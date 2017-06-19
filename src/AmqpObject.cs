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
    using System;
    using System.Threading;
    using Amqp.Framing;

    /// <summary>
    /// The base class of all AMQP objects.
    /// <seealso cref="Session"/>
    /// <seealso cref="SenderLink"/>
    /// <seealso cref="ReceiverLink"/>
    /// </summary>
    public abstract partial class AmqpObject
    {
        internal const int DefaultTimeout = 60000;
        bool closedCalled;
        bool closedNotified;
        Error error;
        ManualResetEvent endEvent;

        /// <summary>
        /// Gets the event used to notify that the object is closed.
        /// </summary>
        public event ClosedCallback Closed;

        /// <summary>
        /// Gets the last <see cref="Error"/>, if any, of the object.
        /// </summary>
        public Error Error
        {
            get
            {
                return this.error;
            }
            internal set
            {
                // try to keep the current error
                if (value != null)
                {
                    this.error = value;
                }
            }
        }

        /// <summary>
        /// Gets a boolean value indicating if the object has been closed.
        /// </summary>
        public bool IsClosed
        {
            get { return this.closedCalled || this.closedNotified; }
        }

        internal bool CloseCalled
        {
            get { return this.closedCalled; }
            set { this.closedCalled = true; }
        }

        internal void NotifyClosed(Error error)
        {
            ManualResetEvent temp = this.endEvent;
            if (temp != null)
            {
                temp.Set();
            }

            if (!this.closedNotified)
            {
                this.closedNotified = true;
                ClosedCallback closed = this.Closed;
                if (closed != null)
                {
                    closed(this, error);
                }
            }
        }

        /// <summary>
        /// Closes the AMQP object. It waits until a response is received from the peer,
        /// or throws TimeoutException after a default timeout.
        /// </summary>
        public void Close()
        {
            this.CloseInternal(DefaultTimeout, null);
        }

        /// <summary>
        /// Closes the AMQP object with the specified error.
        /// </summary>
        /// <param name="timeout">The duration to block until a closing frame is
        /// received from the peer. If it is TimeSpan.Zero, the call is non-blocking.</param>
        /// <param name="error">The AMQP <see cref="Error"/> to send to the peer,
        /// indicating why the object is being closed.</param>
        public void Close(TimeSpan timeout, Error error = null)
        {
            this.CloseInternal((int)(timeout.Ticks / 10000), error);
        }

        internal void CloseInternal(int waitMilliseconds, Error error = null)
        {
            if (this.closedCalled)
            {
                return;
            }

            this.closedCalled = true;
            // initialize event first to avoid the race with NotifyClosed
            if (waitMilliseconds > 0)
            {
                this.endEvent = new ManualResetEvent(false);
            }

            this.Error = error;
            if (!this.OnClose(error))
            {
                if (waitMilliseconds > 0)
                {
                    this.endEvent.WaitOne(waitMilliseconds);
                }
            }
            else
            {
                if (waitMilliseconds > 0)
                {
                    this.endEvent.Set();
                }

                this.NotifyClosed(this.Error);
            }
        }

        /// <summary>
        /// When overridden in a derived class, performs the actual close operation required by the object.
        /// </summary>
        /// <param name="error">The <see cref="Error"/> for closing the object.</param>
        /// <returns>A boolean value indicating if the object has been fully closed.</returns>
        protected abstract bool OnClose(Error error);
    }
}