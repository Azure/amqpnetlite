# Building Application

The [APIs](http://azure.github.io/amqpnetlite/) map directly to the concepts defined in the [AMQP specification](http://docs.oasis-open.org/amqp/core/v1.0/os/amqp-core-overview-v1.0-os.xml). Let's start with a simple example to send a message and receive it back.

## Example: Sync vs. Async
```
Address address = new Address("amqp://guest:guest@localhost:5672");
Connection connection = new Connection(address);
Session session = new Session(connection);

Message message = new Message("Hello AMQP");
SenderLink sender = new SenderLink(session, "sender-link", "q1");
sender.Send(message);

ReceiverLink receiver = new ReceiverLink(session, "receiver-link", "q1");
message = receiver.Receive();
receiver.Accept(message);

sender.Close();
receiver.Close();
session.Close();
connection.Close();
```
On .Net, .Net Core and UWP, it is recommended to use the async APIs.
```
    Address address = new Address("amqp://guest:guest@localhost:5672");
    Connection connection = await Connection.Factory.CreateAsync(address);
    Session session = new Session(connection);

    Message message = new Message("Hello AMQP");    
    SenderLink sender = new SenderLink(session, "sender-link", "q1");
    await sender.SendAsync(message);
	
    ReceiverLink receiver = new ReceiverLink(session, "receiver-link", "q1");
    message = await receiver.ReceiveAsync();
    receiver.Accept(message);

    await sender.CloseAsync();
    await receiver.CloseAsync();
    await session.CloseAsync();
    await connection.CloseAsync();
```
In order to send or receive messages, the application needs to create a Connection, a Session, and one of the SenderLink or ReceiverLink objects. They are explained in details in the following sections.

## Creating a connection
There are two ways to create a connection: through the constructors and through the `ConnectionFactory`.  
Both methods require an Address that specifies where and how to connect. When the address is created from a Uri string, the username and password, if any, must be URL encoded. When the Address is created from individual parts, the parameter values should not be URL encoded.  
When the address contains user info, the SASL PLAIN profile is used to send the credentials; otherwise, the library skips SASL negotiation, unless a SaslProfile is provided to the constructor or set on ConnectionFactory.SASL settings. When SASL PLAIN is used, the application SHOULD ensure a secure transport (e.g. TLS) is used to avoid transmitting credentials in plain text.  
The constructors are blocking until the underlying transport is established. ConnectionFactory provides asynchronous non-blocking creation of a connection, and at the same time it gives more control on the TCP, SSL, SASL and AMQP settings.  
To customize connection's settings, the application can provide its own [Open](http://azure.github.io/amqpnetlite/api/Amqp.Framing.Open.html) object.  
If the application needs to inspect the Open performative from the remote peer, it can provide an OnOpened callback.  
The following example shows,
* SSL without remote certificate validation (maybe for testing purposes).
* SASL anonymous profile.
* a custom Open to define application's container-id and max-frame-size.
* a callback to inspect the remote Open frame.
```
var factory = new ConnectionFactory();
factory.SSL.RemoteCertificateValidationCallback = (a, b, c, d) => true;
factory.SASL.Profile = SaslProfile.Anonymous;
var conneciton = await factory.CreateAsync(
    new Address("amqps://localhost:5671"),
    new Open() { ContainerId = "client.1.2", HostName = "localhost", MaxFrameSize = 8 * 1024 },
    (c, o) => { /*do someting with o*/ });
```
Note that it is NOT guaranteed that the OnOpened callback is invoked after the connection object is returned due to the asynchronous nature of the AMQP protocol. Application can block on waiting for the callback, or process the received Open frame asynchronously.

## Creating a session
Sessions can be created by calling the constructors. Similar to connection, custom Begin object and/or a OnBegin callback can be provided.

## Creating a link
A link is unidirectional. A SenderLink represents a link that transfers messages to the remote peer; a ReceiverLink represents a link that receives messages from the remote peer. A link is created with a session, identified by a unique name, and attached to a node specified by an address.  
Again, application can provide custom Target or Source objects to control the link endpoint behavior, and supply an OnAttached callback to handle the remote attach frame.

## Sending messages
A Message contains multiple sections. At least one section must be initialized before the message is sent. When the message is created with an AMQP serializable object (`Message(object)`), its body is AMQP Value. The object is serialized using the AMQP type system. To control the body type, application can set the BodySection property as follows.
```
var message = new Message() { BodySection = new Data() { Binary = Encoding.UTF8.GetBytes("Hello AMQP") } };
```
Application can do a blocking send by calling `SenderLink.Send(Message, int)`. The call is blocked until an acknowledgement is received or the wait times out. To perform a non-blocking send, application should call `SenderLink.Send(Message, OutcomeCallback, object)`. The OutcomeCallback is invoked when an acknowledgement is received. If OutcomeCallback is null, the library sends the message in best effort mode (i.e. the message is sent pre-settled thus an acknowledgement is not required from the remote peer). Note that even in best effort mode, if the link is detached, an exception will be thrown when Send method is called.

## Receiving messages
Application can do a blocking receive by calling `ReceiverLink.Receive(int)`. The call is blocked until a message is available or the wait times out. Application can alternatively register a callback to process messages by calling `ReceiverLink.Start(int, MessageCallback)`. The callback model eliminates a receive loop in the application.  
By default, the ReceiverLink manages the link credit automatically. The link credit is decremented when message arrives and incremented when application finishes processing it (by calling ReceiverLink.Accept(Message) or ReceiverLink.Reject(Message). Periodecially new credit limit is communicated to the peer in flow frames.  
Application can take control of link flow control by calling ReceiverLink.SetCredit(int, bool) and setting autoRestore to false. In this case, application must keep track of messages received and renew link credit as necessary.

## Shutting down
The connection, session, link objects should be closed when they are no long needed. Typically the AmqpObject.Close(int, Error) method blocks until a response is received. By specifying 0 for waitUntilEnded, the application can initiate the shutdown without waiting for the response.

## Error handling and recovery
The AmqpOjbect provides a Closed event that the application can subscribe to handle errors. This event is raised when the AmqpObject reaches the End state. Application should subscribe to this event as early as possible (e.g. right after the object is created). If the event is subscribed after the object is already closed, the handler is not invoked.
Besides the Closed event, application should handle exception from other API calls. If the object is closed, the API may throw AmqpException with "amqp:illegal-state" error. Application should check the AmqpObject.Error property for error conditions.  
When the application detects the object is closed due to unexpected error, it should perform recovery by recreating the object, and sometimes maybe its container object.

## Threading
Send and receive methods on links are thread safe.  
Many async operation completion and callback invoke are triggered by an incoming frame received from the network. This is performed on the I/O thread where the connection pump is running. Blocking the thread mean no more incoming frames will be processed. So to avoid deadlock or application hang, do not mix sync and async APIs; do not perform blocking calls from an async callback. Below are examples for potential deadlock.
```
    SenderLink sender = new SenderLink(session, "sender", "q1");
    await sender.SendAsync(new Message("m1"));
    sender.Send(new Message("m2"));  // should call SendAsync
```
```
    SenderLink sender = new SenderLink(session, "sender", "q1");
    sender.Send(
        new Message("test"),
        (m, o, s) => ((SenderLink)s).Close(), // should not block
        sender);
```

## Advanced topics
* [Listener](listener.md)
* [Serialization](serialization.md)
* Buffer Management
