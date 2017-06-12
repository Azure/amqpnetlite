using Amqp.Framing;

namespace Amqp
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILink
    {
        /// <summary>
        /// Gets the link name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the link handle.
        /// </summary>
        uint Handle { get; }

        /// <summary>
        /// Gets the session where the link was created.
        /// </summary>
        ISession Session { get; }

        /// <summary>
        /// Gets the last <see cref="Error"/>, if any, of the object.
        /// </summary>
        Error Error { get; }

        /// <summary>
        /// Gets a boolean value indicating if the object has been closed.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Gets the event used to notify that the object is closed.
        /// </summary>
        event ClosedCallback Closed;

        /// <summary>
        /// Closes the AMQP object, optionally with an error.
        /// </summary>
        /// <param name="waitUntilEnded">The number of milliseconds to block until a closing frame is
        /// received from the peer. If it is 0, the call is non-blocking.</param>
        /// <param name="error">The AMQP <see cref="Error"/> to send to the peer, indicating why the object is being closed.</param>
        void Close(int waitUntilEnded = AmqpObject.DefaultCloseTimeout, Error error = null);
    }
}