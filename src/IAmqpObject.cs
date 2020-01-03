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
    using Amqp.Framing;
    using Amqp.Types;

    /// <summary>
    /// The callback that is invoked when an open performative is received from peer.
    /// </summary>
    /// <param name="connection">The connection that is being opened.</param>
    /// <param name="open">The open performative received from the remote peer.</param>
    public delegate void OnOpened(IConnection connection, Open open);

    /// <summary>
    /// The callback that is invoked when a begin performative is received from peer.
    /// </summary>
    /// <param name="session">The session that is being opened.</param>
    /// <param name="begin">The begin performative performative from the remote peer.</param>
    public delegate void OnBegin(ISession session, Begin begin);

    /// <summary>
    /// The callback that is invoked when an attach performative is received from the peer.
    /// </summary>
    /// <param name="link">The link object that is being opened.</param>
    /// <param name="attach">The attach performative received from the remote peer.</param>
    public delegate void OnAttached(ILink link, Attach attach);

    /// <summary>
    /// The callback that is invoked when the AMQP object is closed.
    /// </summary>
    /// <param name="sender">The AMQP object that is closed.</param>
    /// <param name="error">The AMQP <see cref="Error"/>, if any, that caused the object closure.</param>
    public delegate void ClosedCallback(IAmqpObject sender, Error error);

    /// <summary>
    /// A callback that is invoked when an outcome is received from peer for a message.
    /// </summary>
    /// <param name="sender">The link where the message is transferred.</param>
    /// <param name="message">The message to which the outcome applies.</param>
    /// <param name="outcome">The received outcome from the remote peer.</param>
    /// <param name="state">The user object specified in the Send method.</param>
    public delegate void OutcomeCallback(ILink sender, Message message, Outcome outcome, object state);

    /// <summary>
    /// A callback that is invoked when a message is received.
    /// </summary>
    /// <param name="receiver">The receiver link from which a message is received.</param>
    /// <param name="message">The received message.</param>
    public delegate void MessageCallback(IReceiverLink receiver, Message message);

    /// <summary>
    /// Represents an AMQP object.
    /// </summary>
    public partial interface IAmqpObject
    {
        /// <summary>
        /// Gets the event used to notify that the object is closed. Callbacks
        /// may not be invoked if they are registered after the object is closed.
        /// It is recommend to call AddClosedCallback method.
        /// </summary>
        event ClosedCallback Closed;

        /// <summary>
        /// Gets the last <see cref="Error"/>, if any, of the object.
        /// </summary>
        Error Error { get; }

        /// <summary>
        /// Gets a boolean value indicating if the object has been closed.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Adds a callback to be called when the object is closed.
        /// This method guarantees that the callback is invoked even if
        /// it is called after the object is closed.
        /// </summary>
        /// <param name="callback">The callback to be invoked.</param>
        void AddClosedCallback(ClosedCallback callback);

        /// <summary>
        /// Closes the AMQP object. It waits until a response is received from the peer,
        /// or throws TimeoutException after a default timeout.
        /// </summary>
        void Close();

        /// <summary>
        /// Closes the AMQP object with the specified error.
        /// </summary>
        /// <param name="waitUntilEnded">The duration to block until a closing frame is
        /// received from the peer. If it is TimeSpan.Zero, the call is non-blocking.</param>
        /// <param name="error">The AMQP <see cref="Error"/> to send to the peer,
        /// indicating why the object is being closed.</param>
        void Close(TimeSpan waitUntilEnded, Error error);
    }

    /// <summary>
    /// Represents an AMQP connection.
    /// </summary>
    public partial interface IConnection : IAmqpObject
    {
        /// <summary>
        /// Creates a session in the connection.
        /// </summary>
        /// <returns>An ISession object.</returns>
        ISession CreateSession();
    }

    /// <summary>
    /// Represents an AMQP session.
    /// </summary>
    public partial interface ISession : IAmqpObject
    {
        /// <summary>
        /// Creates a sender link in the session.
        /// </summary>
        /// <param name="name">The link name.</param>
        /// <param name="address">The target address where to send messages.</param>
        /// <returns>An ISenderLink object.</returns>
        ISenderLink CreateSender(string name, string address);

        /// <summary>
        /// Creates a receiver link in the session.
        /// </summary>
        /// <param name="name">The link name.</param>
        /// <param name="address">The source address where to receive messages.</param>
        /// <returns>An IReceiverLink object.</returns>
        IReceiverLink CreateReceiver(string name, string address);

        /// <summary>
        /// Creates a sender link in the session.
        /// </summary>
        /// <param name="name">The link name.</param>
        /// <param name="target">The target where to send messages.</param>
        /// <param name="onAttached">The callback that is invoked when an attach performative is received from the peer.</param>
        /// <returns>An ISenderLink object.</returns>
        ISenderLink CreateSender(string name, Target target, OnAttached onAttached = null);

        /// <summary>
        /// Creates a receiver link in the session.
        /// </summary>
        /// <param name="name">The link name.</param>
        /// <param name="source">The source where to receive messages.</param>
        /// <param name="onAttached">The callback that is invoked when an attach performative is received from the peer.</param>
        /// <returns>An IReceiverLink object.</returns>
        IReceiverLink CreateReceiver(string name, Source source, OnAttached onAttached = null);
    }

    /// <summary>
    /// Represents an AMQP link.
    /// </summary>
    public partial interface ILink : IAmqpObject
    {
        /// <summary>
        /// Gets the link name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Detaches the link endpoint without closing it.
        /// </summary>
        /// <param name="error">The error causing a detach.</param>
        /// <remarks>
        /// An exception will be thrown if the peer responded with an error
        /// or the link was closed instead of being detached.
        /// </remarks>
        void Detach(Error error);
    }

    /// <summary>
    /// Represents an AMQP sender link.
    /// </summary>
    public partial interface ISenderLink : ILink
    {
        /// <summary>
        /// Sends a message and synchronously waits for an acknowledgement. Throws
        /// TimeoutException if ack is not received after a default timeout.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void Send(Message message);

        /// <summary>
        /// Sends a message and synchronously waits for an acknowledgement. Throws
        /// TimeoutException if ack is not received in the specified time.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="timeout">The time to wait for the acknowledgement.</param>
        void Send(Message message, TimeSpan timeout);

        /// <summary>
        /// Sends a message asynchronously. If callback is null, the message is sent without
        /// requesting for an acknowledgement (best effort).
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="callback">The callback to invoke when acknowledgement is received.</param>
        /// <param name="state">The object that is passed back to the outcome callback.</param>
        void Send(Message message, OutcomeCallback callback, object state);
    }

    /// <summary>
    /// Represents an AMQP receiver link.
    /// </summary>
    public partial interface IReceiverLink : ILink
    {
        /// <summary>
        /// Starts the message pump.
        /// </summary>
        /// <param name="credit">The link credit to issue.</param>
        /// <param name="onMessage">If specified, the callback to invoke when messages are received.
        /// If not specified, call Receive method to get the messages.</param>
        void Start(int credit, MessageCallback onMessage);

        /// <summary>
        /// Sets a credit on the link. The credit controls how many messages the peer can send.
        /// </summary>
        /// <param name="credit">The new link credit.</param>
        /// <param name="autoRestore">If true, this method is the same as SetCredit(credit, CreditMode.Auto);
        /// if false, it is the same as SetCredit(credit, CreditMode.Manual).</param>
        void SetCredit(int credit, bool autoRestore);

        /// <summary>
        /// Sets a credit on the link and the credit management mode.
        /// </summary>
        /// <param name="credit">The new link credit.</param>
        /// <param name="creditMode">The credit management mode.</param>
        /// <param name="flowThreshold">If credit mode is Auto, it is the threshold of restored
        /// credits that trigers a flow; ignored otherwise.</param>
        void SetCredit(int credit, CreditMode creditMode, int flowThreshold = -1);

        /// <summary>
        /// Receives a message. The call is blocked until a message is available or after a default wait time.
        /// </summary>
        /// <returns>A Message object if available; otherwise a null value.</returns>
        Message Receive();

        /// <summary>
        /// Receives a message. The call is blocked until a message is available or the timeout duration expires.
        /// </summary>
        /// <param name="timeout">The time to wait for a message.</param>
        /// <returns>A Message object if available; otherwise a null value.</returns>
        /// <remarks>
        /// Use TimeSpan.MaxValue or Timeout.InfiniteTimeSpan to wait infinitely. If TimeSpan.Zero is supplied,
        /// the call returns immediately.
        /// </remarks>
        Message Receive(TimeSpan timeout);

        /// <summary>
        /// Accepts a message. It sends an accepted outcome to the peer.
        /// </summary>
        /// <param name="message">The message to accept.</param>
        void Accept(Message message);

        /// <summary>
        /// Releases a message. It sends a released outcome to the peer.
        /// </summary>
        /// <param name="message">The message to release.</param>
        void Release(Message message);

        /// <summary>
        /// Rejects a message. It sends a rejected outcome to the peer.
        /// </summary>
        /// <param name="message">The message to reject.</param>
        /// <param name="error">The error, if any, for the rejection.</param>
        void Reject(Message message, Error error = null);

        /// <summary>
        /// Modifies a message. It sends a modified outcome to the peer.
        /// </summary>
        /// <param name="message">The message to modify.</param>
        /// <param name="deliveryFailed">If set, the message's delivery-count is incremented.</param>
        /// <param name="undeliverableHere">Indicates if the message should not be redelivered to this endpoint.</param>
        /// <param name="messageAnnotations">Annotations to be combined with the current message annotations.</param>
        void Modify(Message message, bool deliveryFailed, bool undeliverableHere, Fields messageAnnotations);
    }

    public partial class AmqpObject : IAmqpObject
    {

    }

    public partial class Connection : IConnection
    {
        ISession IConnection.CreateSession()
        {
            return new Session(this);
        }
    }

    public partial class Session : ISession
    {
        IReceiverLink ISession.CreateReceiver(string name, string address)
        {
            return new ReceiverLink(this, name, address);
        }

        ISenderLink ISession.CreateSender(string name, string address)
        {
            return new SenderLink(this, name, address);
        }

        IReceiverLink ISession.CreateReceiver(string name, Source source, OnAttached onAttached)
        {
            return new ReceiverLink(this, name, source, onAttached);
        }

        ISenderLink ISession.CreateSender(string name, Target target, OnAttached onAttached)
        {
            return new SenderLink(this, name, target, onAttached);
        }
    }

    public partial class Link : ILink
    {
    }

    public partial class SenderLink : ISenderLink
    {
    }

    public partial class ReceiverLink : IReceiverLink
    {
    }
}