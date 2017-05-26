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
