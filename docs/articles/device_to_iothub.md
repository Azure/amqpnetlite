## IoT Hub : connection and device endpoints

The IoT Hub is reachable using an address that has the following format

```
<IOT_HUB_NAME>.azure-devices.net
```

that we can retrieve from the Azure portal after creating the new IoT Hub instance service. As for all the services inside the Service Bus family (queues, topics/subscriptions and event hubs) the IoT Hub needs an SSL/TLS connection for data encryption and server authentication; it means that we have to connect to the host address to the default AMQPS (AMQP Secure) port that is the 5671.

We can create a new device inside the registry and get related credentials information using the Device Explorer application you can download [here](https://github.com/Azure/azure-iot-sdks/blob/master/tools/DeviceExplorer/doc/how_to_use_device_explorer.md). After getting all needed information we can set them into the code.

```
private const string HOST = "IOT_HUB_NAME.azure-devices.net";
private const int PORT = 5671;
private const string DEVICE_ID = "DEVICE_ID";
private const string DEVICE_KEY = "DEVICE_KEY";
```

Using above information we can create an instance of the Address class and using it to establish the connection with the host thanks to the Connection class.

```
address = new Address(HOST, PORT, null, null);
connection = new Connection(address);
```

Inside the IoT Hub architecture, each device has two endpoints for accessing the Cloud :

* D2C (device to cloud) : the device uses this endpoint to send messages to the cloud both as telemetry data and feedback for a received command (on the other endpoint, see below). It means that when we send a command to the device, it replies with a feedback at application level to confirm that the command is acquired and it’s going to be executed. Of course, it’s always true for a rejected command by the device;
* C2D (cloud to device) : the device receives commands on this endpoint for executing the requested action. As already said, the device sends a confirmation (or rejection) of received command to the cloud using the other endpoint (D2C);

At AMQP level the endpoints are accessible from different entity paths; if you know Service Bus queues, topics/subscriptions and event hubs we can think them in the same way.

The entity path for sending data for telemetry purpose is defined in the following way :

```
/devices/<DEVICE_ID>/messages/events
```

where <DEVICE_ID> is the id assigned to the device when we create it inside the identity registry.

The entity path for receiving command from the Cloud is defined in the following way :

```
/devices/<DEVICE_ID>/messages/deviceBound
```

and as for the previous entity you need to provide the <DEVICE_ID> in the path.

It means that after creating a connection and a session to our IoT Hub host we need to create two links to above entities (or nodes as defined in the AMQP spec). Using the programming model provided by AMQP .Net Lite library we have :

* A SenderLink to the /devices/<DEVICE_ID>/messages/events node;
* A ReceiverLink to the /devices/<DEVICE_ID>/messages/deviceBound node;

## Authentication : sending the SAS token

IoT Hub offers a per-device authentication through a SAS token that we can generate starting from device id and device key. After connection establishment we need to send such token to a specific CBS (Claim Based Security) endpoint to authorize the access to the specific entity.

As usual for Azure services, the token has the following format :

```
SharedAccessSignature sig={signature-string}&se={expiry}&skn={policyName}&sr={URL-encoded-resourceURI}
```

The big difference is that the skn field is absent in our case using device credentials .To get the SAS token I used the same code from my Azure SB Lite library because it’s processed almost in the same way.

```
string audience = Fx.Format("{0}/devices/{1}", HOST, DEVICE_ID);
string resourceUri = Fx.Format("{0}/devices/{1}", HOST, DEVICE_ID);
string sasToken = GetSharedAccessSignature(null, DEVICE_KEY, resourceUri, new TimeSpan(1, 0, 0));
bool cbs = PutCbsToken(connection, HOST, sasToken, audience);
```

The PutCbsToken creates a new session and a new link to connect to the specific $cbs node always using the same TCP connection. The content of the message is well defined by the [AMQP CBS draft spec](https://www.oasis-open.org/committees/download.php/50506/amqp-cbs-v1%200-wd02%202013-08-12.doc). After sending the token we are authorized to access IoT Hub from the device.

Just a note : I’m using the Fx class provided by AMQP .Net Lite library to have the Format method that doesn’t exist in the String class for the .Net Micro Framework.

## Sending telemetry data

Using the SenderLink instance the device sends data calling the simple Send() method and passing it a Message class instance contains the data to send.

The sender link is created inside a new AMQP Session (using the related class of AMQP .Net Lite library) and the great news is that, thanks to the multiplexing feature of AMQP protocol, we can use the same session for both sender and receiver links all inside the same TCP connection.

```
static private void SendEvent()
{
    string entity = Fx.Format("/devices/{0}/messages/events", DEVICE_ID);
 
    SenderLink senderLink = new SenderLink(session, "sender-link", entity);
 
    var messageValue = Encoding.UTF8.GetBytes("i am a message.");
    Message message = new Message()
    {
        BodySection = new Data() { Binary = messageValue }
    };
 
    senderLink.Send(message);
    senderLink.Close();
}
```

Running the code, we can interact with the device using the Device Explorer application to receive the messages it sends.

## Receiving command and send feedback

Using the ReceiverLink instance the device can receive command from the service in the Cloud calling the Receive() method. In addition to the sending commands features, the IoT Hub provides a feedback feature at application level for them; it means that the device is able to send a confirmation of received command to the service to accept or reject it. If the device is offline and doesn’t receive the command, the IoT Hub provides a TTL (Time To Live) you can set on every single message so that the command isn’t delivered to the device when it comes back online if the timeout is expired; this feature avoids to deliver a command that makes sense only if it’s executed on the device in a short time.

The device doesn’t need to send the feedback as a message on a specific AMQP node/entity but it’s handled by the IoT Hub when the ReceiverLink accepts or rejects the command. Using AMQP .Net Lite we can call the Accept() or Reject() methods on the ReceiverLink instance; at AMQP level it means that a “disposition” performative is sent to the IoT Hub with an outcome of “accepted” or “rejected”. Receiving this outcome the IoT Hub sends a feedback message to the D2C endpoint on the Cloud service side. With such outcomes the message goes into a completed state (positive feedback to the Cloud) or dead letter state (negative feedback).

```
static private void ReceiveCommands()
{
    string entity = Fx.Format("/devices/{0}/messages/deviceBound", DEVICE_ID);
 
    ReceiverLink receiveLink = new ReceiverLink(session, "receive-link", entity);
 
    Message received = receiveLink.Receive();
    if (received != null)
         receiveLink.Accept(received);
 
    receiveLink.Close();
}
```

Pay attention on the available Release() method in the library; in this case the outcome is “released” and the message returns into the command queue (enqueued state) ready to be re-delivered to the device if it calls the Receive() method again. If the device receives the messages more times and always calls the Release() method, the IoT Hub moves it into the dead letter state (removing it from the command queue) if the messages reaches the max delivery count; the same happens if the device doesn’t call neither Accept() nor Reject() methods and the TTL expires.

Executing the code and using Device Explorer to send the command we can see the feedback from the device too.

## The full source code

The full source code I showed in the previous paragraphs is available on [GitHub](https://github.com/ppatierno/codesamples/tree/master/IoTHubAmqp) and it has projects for .Net Framework (so you can test very quickly it on your PC), generic .Net Micro Framework (for testing on your real device) and a third project for [Netduino 3 WiFi](http://www.netduino.com/netduino3wifi/specs.htm) as example of embedded device.

Of course, you can use any other board that support .Net Micro Framework and SSL/TLS protocol that is needed to connect to the IoT Hub. Other then Netduino 3 board, there are the FEZ Raptor and FEZ Spider from [GHI Electronics](https://www.ghielectronics.com/) (soon an example using them).
