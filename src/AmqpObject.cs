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
    /// <seealso cref="Connection"/>
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
        /// Gets the event used to notify that the object is closed. Callbacks
        /// may not be invoked if they are registered after the object is closed.
        /// It is recommend to call AddClosedCallback method.
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
            get
            {
                lock (this)
                {
                    return this.closedCalled || this.closedNotified;
                }
            }
        }

        internal bool CloseCalled
        {
            get
            {
                lock (this)
                {
                    return this.closedCalled;
                }
            }
            set
            {
                lock (this)
                {
                    this.closedCalled = true;
                }
            }
        }

        internal void NotifyClosed(Error error)
        {
            ManualResetEvent temp = this.endEvent;
            if (temp != null)
            {
                temp.Set();
            }

            ClosedCallback closed;
            lock (this)
            {
                closed = this.Closed;
                bool notified = this.closedNotified;
                this.closedNotified = true;
                if (notified || closed == null)
                {
                    return;
                }
            }

            closed(this, error);
        }

        /// <summary>
        /// Adds a callback to be called when the object is called.
        /// This method guarantees that the callback is invoked even if
        /// it is registered after the object is closed.
        /// </summary>
        /// <param name="callback">The callback to be invoked.</param>
        public void AddClosedCallback(ClosedCallback callback)
        {
            lock (this)
            {
                if (!this.closedNotified)
                {
                    this.Closed += callback;
                    return;
                }
            }

            callback(this, this.error);
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
            lock (this)
            {
                if (this.closedCalled)
                {
                    return;
                }

                this.closedCalled = true;
            }

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
                    try
                    {
                        if (!this.endEvent.WaitOne(waitMilliseconds))
                        {
                            throw new TimeoutException(Fx.Format(SRAmqp.AmqpTimeout,
                                "close", waitMilliseconds, this.GetType().Name));
                        }

                        if (this.error != null && this.error != error)
                        {
                            throw new AmqpException(this.error);
                        }
                    }
                    finally
                    {
                        this.NotifyClosed(this.Error);
                    }
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