In messaging applications, fault tolerance is important. Communication could fail due to many error conditions,
such as networking failure, service temporary unavailability and planned/unplanned maintenance. Applications
must handle these errors and be able to recover from them.

As error handling and reconnect logic are very application specific, the library does not provide a built-in
retry or recovery logic. Instead it provides several important mechanisms for application to build fault
tolerance, such as,
* Exceptions and error conditions,
* State and terminal error on AMQP objects,
* Closed event on AMQP objects.

## Exceptions

Application should expect exceptions being thrown from API calls and handle them. The library defines the
`AmqpException` type and throws the exception whenever appropriate. Other common exceptions, such as
`ArgumentException` and `TimeoutException`, could also be thrown.

When an `AmqpException` is handled, application should check its `Error` property, specifically the `Condition`
property of the `Error` object and perform actions accordingly. Please refer to the AMQP specification for the
standard error conditions, and the extended error conditions, if any, defined by the remote peer that the
application is communicating with.

## AmqpObject

When an `AmqpObject` (`Connection`, `Session`, `Link`) has transitioned to closing/ending/detaching state,
any operation other than close will trigger an AmqpException with error condition "amqp:illegal-state" to
be thrown.

The `AmqpObject.Error` property, if set, indicates the error condition under which the object was closed.

The `AmqpObject.IsClosed` property also tells if the object has been closed.

The `AmqpObject.Closed` event notifies subscribers when the object reaches the end state.

## Reconnect

Using the primitives provided above, application can build a reconnect logic that fits the best to its
runtime environment. Following are the typical ways to built such reconnect logic. You can find all of
them in the [LongHaulTest](https://github.com/Azure/amqpnetlite/tree/master/test/LongHaulTest) project.

### Proactive State Check

Before calling any API, the application can check `AmqpObject.IsClosed` property and create the object
if it is closed.

For example, the LongHaulTest uses this strategy to create connection before calling Send or
Receive methods. Similar approach is taken to ensure a link is in place.
```
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
```

### Handle Exceptions

Exceptions must be handled. In the exception handler, the application has a choice of whether to create
the communication objects, or simply delegate the recreation to the Proactive State Check
strategy if one is in place.

### Subscribe to Closed Event

Application can also subscribe to the Closed event and perform reconnect logic in the event handler.
the Closed event handler is guaranteed to be invoked at most once, as the object could be closed already
when the event is subscribed. Application can use the following logic to avoid the race condition
and ensure the event handler is always invoked. Note that the guarantee becomes at least once so
the event handler must be idempotent.
```
void SafeAddClosed(AmqpObject obj, ClosedCallback callback)
{
    obj.Closed += callback;
    if (obj.IsClosed)
    {
        callback(obj, obj.Error);
    }
}
```

If the application ever decides to create the AMQP object(s) in the exception handler or Closed event
handler, it **must not mix sync and async API calls**. Most async API calls are completed when a
response is received from the peer and the execution is on the connection's frame pump (which runs on
the I/O thread). If a sync operation which also requires a response is performed, a deadlock occurs.
Most likey you will get a TimeoutException.

The application can use a combination of the above approaches to achieve optimal results, like what
has been done in the LongHaulTest project to enable it to run for weeks against an Azure Service Bus
queue and transfers tens of millions message between the senders and the receivers.
