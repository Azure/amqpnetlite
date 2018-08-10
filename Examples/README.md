## AmqpNetLite C# Examples
This directory contains example C# programs using the library. Some of the examples require an AMQP 1.0 broker with pre-configured queues. The examples are organized as follows:
* Device: examples for NETMF, nanoFramework and Windows Phone.
* Interop: clients interoperating with a broker in different patterns.
* Listener: usage of the listener APIs in server or broker applications.
* PeerToPeer: peer-to-peer communication.
* ServiceBus: clients interoperating with the Azure Service Bus service.

### Device Examples
| Project | Description |
|---------|:-----------|
| Device.Controller | A Windows Phone 8.0 app that reads temprature data from a "data" queue and sends commands to a "control" queue to adjust the temprature. |
| Device.Controller2 | Same as Device.Controller but for Windows Phone 8.1. |
| Device.Thermometer | A NETMF app that displays current temprature, sends it to a "data" queue, and reads commands from a "control" queue to change the temprature. |
| Device.Thermometer.nanoFramework | A nanoFramework app that displays current temprature, sends it to a "data" queue, and reads commands from a "control" queue to change the temprature. |
| Device.SmallMemory | A NETMF app that uses the Amqp.Micro.NetMF client to send and receive data to the Azure IoT Hub service. |
| Device.SmallMemory.nanoFramework | A nanoFramework app that uses the Amqp.Micro.NetMF client to send and receive data to the Azure IoT Hub service. |

### Interop Examples
| Project | Description |
|---------|:-----------|
| Interop.Client | A request-response client |
| Interop.Server | A request-response server |
| Interop.Spout | A message sender client |
| Interop.Drain | A message receiver client |

### Listener Examples
| Project | Description |
|---------|:-----------|
| Listener.ContainerHost | Usage of ILinkProcessor and LinkEndpoint |
| Listener.IContainer | Usage of IContainer to build a fully functional AMQP 1.0 broker |

### PeerToPeer Examples
| Project | Description |
|---------|:-----------|
| PeerToPeer.Client | A client sending requests to remote peer and receiving responses |
| PeerToPeer.Server | A listener replying to client requests. The client and server examples also demonstrate recovery from connection failures. |
| PeerToPeer.Receiver | A receiver reading CPU and memory data from an monitoring endpoint registered with a container host. |
| PeerToPeer.Certificate | Mutual authentication of the client (a message sender) and the listener using X509 certificates. |
| PeerToPeer.CustomType | Usage of the built-in serializer to send .Net objects to remote peer (or even a broker). |

### ServiceBus Examples
A valid credential (e.g. SAS policy configured on the entity or namespace) is required for the client to connect. The entity that the samples run against must also
exist under the specified Service Bus namespace. You can create the entity using the Service Bus .Net SDK or through the Azure portal.

| Project | Description |
|---------|:-----------|
| ServiceBus.EventHub | Sending to Event Hub with and without partition key, partition and publisher; receiving from partitions. Mapping of AMQP message to EventData. |
| ServiceBus.EventHub.NetMF | Interacting with Event Hubs service on NETMF. |
| ServiceBus.Cbs | Authentication with CBS (Claim Based Security) |
| ServiceBus.MessageSession | Working with MessageSession in Service Bus |
| ServiceBus.Topic | Working with Topic and Subscription in Service Bus |
