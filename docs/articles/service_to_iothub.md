## IoT Hub service endpoints

Inside the IoT Hub architecture, the service has two endpoints to communicate with devices :

* C2D (cloud to device) : the back end system can use this endpoint to send messages (for example commands) to the devices. This endpoint acts like a queue and each message has a TTL (Time To Live) so that it’s removed from the queue if the timeout expires (it’s useful to have commands executed in a short period of time and not executed too late when an offline device comes back online but the execution isn’t needed at that time because it could be harmful). The back end system can receive a confirmation message or delivery fault to understand if device has received command or not;
* D2C (device to cloud) : it’s an Event Hubs compatible endpoint used by the back end system to retrieve messages from device (telemetry data) and feedback on command delivery (successful or not). “Event Hubs compatible” means that we can use an Event Hub client to receive messages from this endpoint (for example using an Event Processor Host implementation);

At AMQP level the endpoints are accessible from different entity paths; if you know Service Bus queues, topics/subscriptions and event hubs we can think them in the same way.

The entity path for sending command to devices is defined in the following way :

```
/messages/devicebound
```

while the entity path for receiving feedback (on commands sent) from devices is the following :

```
/messages/servicebound/feedback
```

As for the previous article, it means that after creating a connection and a session to our IoT Hub host we need to create two links to above entities (or nodes as defined in the AMQP spec). Using the programming model provided by AMQP .Net Lite library we have :

* A SenderLink to the /messages/devicebound node;
* A ReceiverLink to the /messages/servicebound/feedback node;

## Authentication : sending the SAS token

The authentication mechanism is the same as device side. In this scenario, we need to send two SAS token on the two different AMQP nodes for sending command and receiving feedback.

The SAS token audience and resource URI for sending command are the same and defined in the following way :

```
string audience = Fx.Format("{0}/messages/devicebound", HOST);
string resourceUri = Fx.Format("{0}/messages/devicebound", HOST);
string sasToken = GetSharedAccessSignature(SHARED_ACCESS_KEY_NAME, SHARED_ACCESS_KEY, resourceUri, new TimeSpan(1, 0, 0));
bool cbs = PutCbsToken(connection, HOST, sasToken, audience);
```

For receiving feedback, they are the following :

```
string audience = Fx.Format("{0}/messages/servicebound/feedback", HOST);
string resourceUri = Fx.Format("{0}/messages/servicebound/feedback", HOST);
string sasToken = GetSharedAccessSignature(SHARED_ACCESS_KEY_NAME, SHARED_ACCESS_KEY, resourceUri, new TimeSpan(1, 0, 0));
bool cbs = PutCbsToken(connection, HOST, sasToken, audience);
```

## Sending command

Using the SenderLink instance the device sends data calling the simple Send() method and passing it a Message class instance contains the data to send.

The sender link is created inside a new AMQP Session (using the related class of AMQP .Net Lite library) and the great news is that, thanks to the multiplexing feature of AMQP protocol, we can use the same session for both sender and receiver links all inside the same TCP connection.

The corresponding class in the official SDK is the ServiceClient class that provides the SendAsync() method. Regarding the original Message class (included into official SDK, not AMQP .Net Lite), it exposes the Ack property with following possible values :

* none (default) : the service doesn’t want any feedback on command received by the device;
* positive : the service receives a feedback message if the message was completed;
* negative : the service receives a feedback message if the message expired (or max delivery count was reached) without being completed by the device;
* full : the service receives both positive and negative feedbacks;

For more information you can refer to the previous article with a clear explanation of the message life cycle.

Using the AMQP .Net Lite library we don’t have an Ack property on the Message class but we need to use the application properties collection at AMQP level. The Ack property (at high level) is translated in an application property named “iothub-ack” (at AMQP level) which can have the above possible values. If we don’t set this application property, it means the same as “none” value so no feedback.

```
static private void SendCommand()
{
    string audience = Fx.Format("{0}/messages/devicebound", HOST);
    string resourceUri = Fx.Format("{0}/messages/devicebound", HOST);
 
    string sasToken = GetSharedAccessSignature(SHARED_ACCESS_KEY_NAME, SHARED_ACCESS_KEY, resourceUri, new TimeSpan(1, 0, 0));
    bool cbs = PutCbsToken(connection, HOST, sasToken, audience);
 
    if (cbs)
    {
         string to = Fx.Format("/devices/{0}/messages/devicebound", DEVICE_ID);
         string entity = "/messages/devicebound";
 
         SenderLink senderLink = new SenderLink(session, "sender-link", entity);
 
         var messageValue = Encoding.UTF8.GetBytes("i am a command.");
         Message message = new Message()
         {
              BodySection = new Data() { Binary = messageValue }
         };
         message.Properties = new Properties();
         message.Properties.To = to;
         message.Properties.MessageId = Guid.NewGuid().ToString();
         message.ApplicationProperties = new ApplicationProperties();
         message.ApplicationProperties["iothub-ack"] = "full";
 
         senderLink.Send(message);
         senderLink.Close();
    }
}
```

As we can see, the sending path “/messages/devicebound” hasn’t any information about the target device. To do that, the service need to set the To AMQP system property to the following value :

```
/devices/<DEVICE_ID>/messages/devicebound
```

where <DEVICE_ID> is the id assigned to the device when we create it inside the identity registry.

Finally, it’s importat to notice that the C2D endpoint queue can hold at most 50 messages.

## Receiving feedback

Using the ReceiverLink instance the service can receive feedback from the device calling the Receive() method.

```
static private void ReceiveFeedback()
{
     string audience = Fx.Format("{0}/messages/servicebound/feedback", HOST);
     string resourceUri = Fx.Format("{0}/messages/servicebound/feedback", HOST);
 
     string sasToken = GetSharedAccessSignature(SHARED_ACCESS_KEY_NAME, SHARED_ACCESS_KEY, resourceUri, new TimeSpan(1, 0, 0));
     bool cbs = PutCbsToken(connection, HOST, sasToken, audience);
 
     if (cbs)
     {
          string entity = "/messages/servicebound/feedback";
 
          ReceiverLink receiveLink = new ReceiverLink(session, "receive-link", entity);
 
          Message received = receiveLink.Receive();
          if (received != null)
          {
               receiveLink.Accept(received);
               System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(received.GetBody<byte[]>()));
          }
 
          receiveLink.Close();
     }
}
```

The received message has a body in JSON format with an array of records (feedback from more different devices) each with following properties :

* OriginalMessageId : it’s the MessageId of the original command (message) sent from the service to the device;
* Description : description result that is related to the possible outcomes (success, message expired, maximum delivery count exceeded, message rejected);
* DeviceGenerationId : device generation id related to the device that sent the feedback for a specific command;
* DeviceId : device id related to the device that sent the feedback for a specific command;
* EnqueuedTimeUtc : timestamp related to the outcome (it means when the feedback was enqueued);

For a single feedback, the JSON should be as following :

```
[{"originalMessageId":"5aac3169-af00-4536-acdb-cb9ea6b3980e","description":"Success","deviceGenerationId":"635794823643795743","deviceId":"<device_id>","enqueuedTimeUtc":"2015-10-29T07:59:00.9772497Z"}]
```

## The full source code

As for all examples related to my blog posts, I update sample from previous article on [GitHub](https://github.com/ppatierno/codesamples/tree/master/IoTHubAmqp). Now you can find a simple console application and a UWP application that are able to send command to a device and receive related feedback.
