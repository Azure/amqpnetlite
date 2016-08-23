# Buffer Manager

On .Net Framework (4.0 and above) and .Net Core, the library supports buffer pooling to reduce allocation and improve performance.

## IBufferManager

Application can implement this interface to manage byte array allocation and pooling. There are different pooling strategies.
* Allocate fixed-size byte[] objects and create smaller pools of each size.
* Allocate a large byte array and manage it like a heap.

The library provides a built-in buffer manager (`BufferManager`) which should be sufficient for most scenarios. If the application has a buffer manager for application data, it makes sense to use it as well for the library.

The buffer manager can be set to `ConnectionFactory.BufferManager` or `ConnectionListener.BufferManager`. Once it is set, most if not all byte array allocations will be through the buffer manager.

```
ConnectionFactory factory = new ConnectionFactory();
factory.BufferManager = new BufferManager(64, 1024 * 1024, 50 * 1024 * 1024);
```
In this example, a buffer manager is created to pool buffers from 64 bytes to 1 MB with a limit of 50 MB on total memory usage.

## Application Responsibility

### Sending Messages

In most cases, no special actions are required for sending messages. However, if the application has a lot of binary data to transfer, it should take advantage of `Data.Buffer` to reduce allocation. `Data.Buffer` is of type `ByteBuffer` which can wrap an existing `byte[]` or `ArraySegment<byte>` which in turn can be taken from the buffer manager. The following example shows how to use pooled buffers to initialize message for send.
```
ArraySegment<byte> segment = bufferManager.TakeBuffer(1024);
int len = WriteBuffer(segment);  // initialize the buffer and return payload length
ByteBuffer buffer = new ByteBuffer(segment.Array, segment.Offset, len, segment.Count);
Message message = new Message() { BodySection = new Data() { Buffer = buffer } };
try
{
    await sender.SendAsync(message);
}
finally
{
    bufferManager.ReturnBuffer(segment);
}
```

### Receiving Messages

The received message is backed up by a buffer taken from the buffer manager. The ownership of the message is transferred to the application after the receive call. Therefore, the application must ensure that `Message.Dispose` is called after the message is processed, otherwise a buffer is leaked (not returned to the buffer manager). To access the message body, the application should avoid reading `Message.Body` or `((Data)Message.BodySection).Binary`. Internally the library wraps the body payload in a `ByteBuffer` object, and it requires an allocation of `byte[]` with the exact size and a copy of the payload into the `byte[]` object. The recommended way to access the body is to get back the `ByteBuffer` object and process it as follows.
```
Message message = await receiver.ReceiveAsync();
try
{
    ByteBuffer buffer = message.GetBody<ByteBuffer>();
    await ProcessBufferAsync(buffer);
    receiver.Accept(message);
}
finally
{
    message.Dispose();
}
```
