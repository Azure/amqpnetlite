The Azure Service Bus Event Hubs supports the standard AMQP 1.0 protocol. The only thing specific to the Event Hubs is the addressing scheme and message properties (the following assumes you created an Event Hub named "myeventhub").

## Addressing
* Send to event hub: attach.target.address = "myeventhub"  
* Send to event hub partition "0": attach.target.address = "myeventhub/Partitions/0"  
* Send to a publisher endpoint: attach.target.address = "myeventhub/Publishers/device1"  
* Receive from the default consumer group of partition "0": attach.source.address = "myeventhub/ConsumerGroups/$default/Partitions/0"  

## Message properties
In addition to the standard AMQP message properties, the Event Hub defines the following properties stored in the message annotations section (the keys are AMQP symbols). Only the partition key property can be set by the sender.  
* “x-opt-partition-key”: (string) specifies a partition key used for message partitioning. When the target address is the event hub name, this value is used to compute a hash to select a partition to send the message to.  
* “x-opt-offset”: (string) specifies an opaque pointer of the message in the event stream.  
* “x-opt-sequence-number”: (long) specifies the sequence number of the message in the event stream.  
* “x-opt-enqueued-time”: (timestamp) specifies when the message was enqueued.  
* “x-opt-publisher”: (string) specifies the publisher name if the message was sent to a publisher endpoint.  

## Filter
A filter specifies the position (in attach.source.filter-set) where the receiver wants to start receiving.  
key: symbol(“apache.org:selector-filter:string”)  
value: a described string: descriptor=symbol(“apache.org:selector-filter:string”), value is an expression.  Examples,  
* Start from message at offset 100 exclusively  
`”amqp.annotation.x-opt-offset > '100'”`
* Start from message at offset 100 inclusively  
`”amqp.annotation.x-opt-offset >= '100'”`
* Start from message received after a timestamp. The number is an AMQP timestamp.  
`”amqp.annotation.x-opt-enqueued-time > 1234567”`

The offset filter is perferred as it is more performant than the enqueued-time filter. The enqueued-time filter is for rare cases when you lose the checkpoint data and have to go back a certain period of time in history. The following special offsets are defined.
'-1': beginning of the event stream.
'@latest': end of the even stream, in other words, all new events after the link is attached.

## Batching in message sender
Azure Event Hubs supports an extended message format (0x80013700) which allows a sender to pack multiple messages into one AMQP message.
It is intended to help applications publish messages more efficiently, especially with small messages and high-latency networks.
The envelop message is a standard AMQP 1.0 message with multiple Data sections, each of which contains one encoded payload message in its
binary value. On the service side, the payload messages are extracted and delivered to receivers individually.
The `MessageBatch` class in the test project illustrates how such batch messages can be created.
```
    public class MessageBatch : Message
    {
        public const uint BatchFormat = 0x80013700;

        public static MessageBatch Create<T>(IEnumerable<T> objects)
        {
            DataList dataList = new DataList();
            foreach (var obj in objects)
            {
                ByteBuffer buffer = new ByteBuffer(1024, true);
                var section = new AmqpValue<T>(obj);
                AmqpSerializer.Serialize(buffer, section);
                dataList.Add(new Data() { Buffer = buffer });
            }

            return new MessageBatch() { Format = BatchFormat, BodySection = dataList };
        }
    }
```