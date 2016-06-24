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

    /// <summary>
    /// The callback that is invoked when the AMQP object is closed.
    /// </summary>
    /// <param name="sender">The AMQP object.</param>
    /// <param name="error">The AMQP <see cref="Error"/>, if any.</param>
    public delegate void ClosedCallback(AmqpObject sender, Error error);

    /// <summary>
    /// The base class of all AMQP objects.
    /// <seealso cref="Session"/>
    /// <seealso cref="SenderLink"/>
    /// <seealso cref="ReceiverLink"/>
    /// </summary>
    public abstract class AmqpObject
    {
        internal const int DefaultCloseTimeout = 60000;
        bool closedCalled;
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
            get;
            internal set;
        }

        internal void NotifyClosed(Error error)
        {
            ManualResetEvent temp = this.endEvent;
            if (temp != null)
            {
                temp.Set();
            }

            if (!this.closedCalled)
            {
                this.closedCalled = true;
                ClosedCallback closed = this.Closed;
                if (closed != null)
                {
                    closed(this, error);
                }
            }
        }

        /// <summary>
        /// Closes the AMQP object, optionally with an error.
        /// </summary>
        /// <param name="waitUntilEnded">The number of milliseconds to block until a closing frame is
        /// received from the peer. If it is 0, the call is non-blocking.</param>
        /// <param name="error">The AMQP <see cref="Error"/> to send to the peer, indicating why the object is being closed.</param>
        public void Close(int waitUntilEnded = DefaultCloseTimeout, Error error = null)
        {
            // initialize event first to avoid the race with NotifyClosed
            this.endEvent = new ManualResetEvent(false);
            this.Error = error;
            if (!this.OnClose(error))
            {
                if (waitUntilEnded > 0)
                {
                    this.endEvent.WaitOne(waitUntilEnded);
                }
            }
            else
            {
                this.endEvent.Set();
                this.NotifyClosed(null);
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