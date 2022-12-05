The goal of AmqpNetLite is to provide a library that runs on every possible .Net platform, and
is simple to use but also gives full control of the AMQP protocol when needed. Domain knowledge
of AMQP can help your start with the library but is not mandatory.

The [APIs](http://azure.github.io/amqpnetlite/) are a direct mapping to the concepts defined in
the [AMQP specification](http://docs.oasis-open.org/amqp/core/v1.0/os/amqp-core-overview-v1.0-os.xml).
If you have not read the [Hello AMQP!](hello_amqp.md) guide, take a few minutes to go through it and
see how easy it is to exchange messages between your applications.

In this article, we will be explaining in details of how the library can be used and some best practices
you, as an application developer, need to be aware of.

AMQP messages are transferred over a link created in a bi-directional session channel, which is also
created in a connection. In order to send or receive messages, the application needs to set up
the protocol artifacts by creating objects of various classes in the library. The class names are
self-explanatory (e.g. Connection, Session, SenderLink, ReceiverLink).

## Specifying an Address

The AMQP endpoint where the client is connecting is represented as an Address object. An Address
object can be created from a Uri string, or individual parameters specifying different parts of
the Uri.
* When a Uri string is used, the username and password, if any, must be URL encoded.
* When multiple parameters are used, they must not be URL encoded.

In addition to the TCP endpoint (host and port), the Address object also specifies how the connection
handshake should take place.
* The Scheme property determines if a secure channel (TLS/SSL) should be established.
* The user info part determines if authentication (AMQP SASL) should be performed after the transport
is established.
* When user info is absent, the library skips SASL negotiation all together. Please do not assume
that SASL ANONYMOUS will be used.
* The Path property is only used in WebSockets transport ("ws" and "wss").
* The Host property is set on the open.hostname field during connection negotiation.

## Creating a connection

There are two ways to create a connection:
* through the constructors.
* through the `ConnectionFactory`.  

### Constructors

`public Connection(Address address)`  
This is probably the easiest way to create a connection to the given address. The Address object
fully determines how the connection is established as explained in [Specifying an Address](#specifying-an-address)
section.

`public Connection(Address address, SaslProfile saslProfile, Open open, OnOpened onOpened)`  
This constructor can be used in cases where the application needs more control, such as
* providing a SaslProfile to perform specific SASL negotation, regardless of the user info, if any, in the address.
* providing an Open performative for connection negotiation. It allows the application
to set desired values on Open, e.g. hostname, container-id, idle-timeout, custom properties, and so on.
The application must ensure that the mandatory field, ContainerId, is set to avoid connection failures.
* providing an OnOpened callback to handle the Open performative from the remote peer.

The following example demonstrates the above usages.
```
var connection = new Connection(
    new Address("amqps://contoso.com:5671"),
	SaslProfile.Anonymous,
    new Open() { ContainerId = "client.1.2", HostName = "contoso.com", MaxFrameSize = 8 * 1024 },
    (c, o) => { /* do someting with remote Open o */ });
```

Keep in mind that the OnOpened callback may be invoked before the constructor returns due to
the asynchronous nature of the AMQP protocol. It is not guaranteed to be invoked after the
constructor returns either. If the application requires the remote Open performative to be
processed before proceeding, it can wait on a task or an event which is completed or set in
the OnOpened callback.

The constructors are blocking until the underlying transport is established. This may cause
undesired behavior, e.g. UI freeze if it is called from a UI thread. In these cases, application
should use the `ConnectionFactory` class to perform asynchronous non-blocking creation of
a connection.

On platforms where `ConnectionFactory` is supported, the default factory (`Connection.Factory`) is
used for all connections created by the constructors. Therefore, any change made to the settings of
the default factory is applied to connections created afterwards.

### ConnectionFactory

`ConnectionFactory` provides asynchronous connection creation, and it also gives more control
on the transport and AMQP protocol.

If you only need to create a connection asynchronously, do the following.
```
var connection = await Connection.Factory.CreateAsync(address);
```

Optionally you can give the Open and OnOpened callback in the same way as in using constructors.
```
var connection = await Connection.Factory.CreateAsync(
    new Address("amqps://localhost:5671"),
    new Open() { ContainerId = "client.1.2", HostName = "localhost", MaxFrameSize = 8 * 1024 },
    (c, o) => { /*do someting with o*/ });
```

To control the transport behavior and the AMQP protocol settings, you can create a `ConnectionFactory`
object and configure the TCP, SSL, SASL and AMQP properties of the object. For example,
```
ConnectionFactory factory = new ConnectionFactory();
factory.TCP.NoDelay = true;
factory.TCP.SendBufferSize = 16 * 1024;
factory.TCP.SendTimeout = 30000;
factory.SSL.ClientCertificates = GetClientCert();
factory.SASL.Profile = SaslProfile.External;
factory.AMQP.MaxFrameSize = 64 * 1024;
factory.AMQP.HostName = "contoso.com";
factory.AMQP.ContainerId = "container:" + testName;
var connection = await factory.CreateAsync(new Address("amqps://localhost:5671"));
```

### TLS/SSL

Here are some special notes about TLS/SSL.
* It is enabled when the address scheme is "amqps".
* It is enabled when any properties of `ConnectionFactory.SSL` is set, regardless of the address scheme.
* In testing, you may need to disable server certificate validation. This is done by one of the following.
`Connection.DisableServerCertValidation = true;` or  
`factory.SSL.RemoteCertificateValidationCallback = (a, b, c, d) = true;`
* In production, do not disable remote certificate validation as it presents a security risk.
* TLS/SSL SHOULD be used when SASL PLAIN is used to avoid sending credentials in plain text.
* On NETMF, TSL/SSL works only when it is supported by the device.

## Creating a Session

Sessions can be created by calling the constructors. Similar to connection, custom Begin object
and/or an OnBegin callback can be provided.

## Creating a Link

A link is unidirectional. A SenderLink represents a link that transfers messages to the remote peer;
a ReceiverLink represents a link that receives messages from the remote peer. A link is created in
a session, identified by a unique name, and attached to a node specified by an address.  
Again, application can provide custom Target or Source objects to control the link endpoint behavior,
and supply an OnAttached callback to handle the remote attach frame.

## Sending Messages

A Message contains multiple sections. At least one section must be initialized before the message is sent.
When the message is created with an AMQP serializable object (`Message(object)`), its body is set to
be and AMQP Value.
The object is serialized using the AMQP type system. To control the body type, application can set the
BodySection property as follows.
```
var message = new Message() { BodySection = new Data() { Binary = Encoding.UTF8.GetBytes("Hello AMQP") } };
```
Application can do a blocking send by calling `SenderLink.Send(Message, int)`. The call is blocked until
an acknowledgement is received or the wait time elapses.  
To perform a non-blocking send, application should call `SenderLink.Send(Message, OutcomeCallback, object)`.
The OutcomeCallback is invoked when an acknowledgement is received.  
If OutcomeCallback is null, the library sends the message in best effort mode or fire-and-forget mode.
```
SenderLink.Send(Message, null, null)
```
It sends the message pre-settled thus an acknowledgement is not required from the remote peer.
Note that even in best effort mode, if the link is detached, an exception is thrown when Send method is called.

## Receiving Messages

### Receive Loop

Application can do a blocking receive by calling `ReceiverLink.Receive(int)`. The call is blocked until a message
is available or the wait time elapses.  
To continously receive message, application calls the `Receive` method in a loop.

### MessageCallback

Application can alternatively register a callback to process messages by calling `ReceiverLink.Start(int, MessageCallback)`.
The callback model eliminates a receive loop in the application.

### Link Credit

Link credit is a mechanism for controlling message flow in AMQP. A receiver issues credits to the remote peer and
the credits determine the maximum number of messages the remote peer can send to the receiver.  
By default, the ReceiverLink class manages the link credit automatically. The link credit is decremented
when message arrives and incremented when application finishes processing it (by calling `ReceiverLink.Accept(Message)`
or `ReceiverLink.Reject(Message)`). Periodically new credit limit is communicated to the peer in flow frames.  
Application can set the initial link credit by calling `ReceiverLink.SetCredit(int, true)` before calling the `Receive`
method, or setting the value in the Start method when registering the message callback.  
Application can also fully take over flow control by calling `ReceiverLink.SetCredit(int, false)`. When autoRestore is
false, the library stops sending flow performative. The application must keep track of received messages and
renew link credit as necessary by calling the `SetCredit` method.

## Shutting Down

The connection, session, link objects should be closed when they are no long needed. To close them, simple call the
`AmqpObject.Close` method. The `Close` method blocks until a response is received. By specifying a value of 0
for waitUntilEnded, the application can initiate the shutdown without waiting for the response.

Closing an object automatically closes all contained objects. For example, when a Connection is closed, all sessions
in that connection is automatically closed.

## Sync vs. Async

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

## Error Handling and Recovery

The AmqpOjbect provides a Closed event that the application can subscribe to handle errors.
This event is raised when the AmqpObject reaches the End state. Application should subscribe to
this event as early as possible (e.g. right after the object is created). If the event is
subscribed after the object is already closed, the handler is not invoked.  
Besides the Closed event, application should handle exceptions from the API calls.
If the object is closed, the API may throw AmqpException with "amqp:illegal-state" error.
Application should check the AmqpObject.Error property for error conditions.  
When the application detects the object is closed due to unexpected error, it should perform
recovery by recreating the object, and sometimes maybe its container object.

## Threading

Send and receive methods on links are thread safe.

The library does not create any threads for sending or receiving messages. Instead the async API relies on an asynchronous connection "pump", 
meaning a continous loop that asynchronously processes I/O. It is critical for proper operation that the application does not block this pump. 

There are two ways in which an application can accidentally block the pump. The first is by performing a blocking operation in a callback:

```
SenderLink sender = new SenderLink(session, "sender", "q1");
sender.Send(
    new Message("test"),
    (m, o, s) => Thread.Sleep(120000),
    sender);
```

The second occurs when an async operation is completed and its continuation runs *synchronously*:

```
SenderLink sender = new SenderLink(session, "sender", "q1");
await sender.SendAsync( new Message("test"));

Thead.Sleep(120000);
```

The above code will block the pump if the continuation of SendAsync happens to be run synchronously (which it frequently will be). 
The problem is the same as with the first example, the async pump does not get a chance to release the currently executing thread back to the 
thread pool and therefore has no chance to await further I/O. This means that no messages can be processed and will typically cause deadlock, 
application hang or timeout errors.  

Specifically it is important to understand that blocking operations to be avoided include the sync API of this library:
```
SenderLink sender = new SenderLink(session, "sender", "q1");
await sender.SendAsync(new Message("m1"));
sender.Send(new Message("m2"));
```
Here, the Send call will timeout because the returned acknowledgement cannot be processed when the I/O
processing is blocked.

The solution for callbacks is naturally to schedule blocking work asynchronously instead of blocking the calling thread. The solution for continuations 
depends on the application, which must either ensure that continuations of async operations never occur on threads that do blocking work, or the async operations can be wrapped by the application using a `TaskCompletionSource` with `TaskCreationOptions.RunContinuationsAsynchronously` specified.

For more details see the following issues:
https://github.com/Azure/amqpnetlite/issues/237
https://github.com/Azure/amqpnetlite/issues/490

## Advanced topics
* [Listener](listener.md)
* [Serialization](serialization.md)
* [Buffer Management](buffer_management.md)
* [Fault Tolerance](fault_tolerance.md)
