This guide shows you how to exchange messages between two programs through a broker with Amqp.Net Lite.

## Prerequisites
First you need an AMQP 1.0 compliant broker. You can use any of the following:
* [Apache ActiveMQ](http://activemq.apache.org/)
* [Microsoft Azure Service Bus](https://azure.microsoft.com/en-us/services/service-bus/)
* [Apache Qpid Java Broker](http://qpid.apache.org/components/java-broker/)

You will also need valid credentials and a pre-configured queue (or other types of entities)
in the broker. If you do not have any broker to start with and just want to quickly try out
AMQP on .Net, you can download the [TestAmqpBroker](https://github.com/Azure/amqpnetlite/releases/download/test_broker.1609/TestAmqpBroker.zip)
which is built on top of this library. After downloading the zip file, unzip it and execute
the following command.  
`TestAmqpBroker.exe amqp://localhost:5672 /creds:guest:guest /queues:q1`

The test broker now listens on 0.0.0.0:5672 and accepts AMQP connections with "guest:guest" as
user name and password. A queue named "q1" is also pre-created.

## Projects and References
Now open Visual Studio and create two Console Application projects: Sender and Receiver.

Open NuGet Package Manager. Browse for "AmqpNetLite". Install the latest version to both projects.
If you prefer using the NPM console, you can do this by executing:
```
Install-Package AmqpNetLite -ProjectName Sender
Install-Package AmqpNetLite -ProjectName Receiver
```

## Code and Build

In Sender project, open Program.cs file and type in the following code.
```
using System;
using Amqp;

namespace Sender
{
    class Program
    {
        static void Main(string[] args)
        {
            Address address = new Address("amqp://guest:guest@localhost:5672");
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            Message message = new Message("Hello AMQP!");
            SenderLink sender = new SenderLink(session, "sender-link", "q1");
            sender.Send(message);
            Console.WriteLine("Sent Hello AMQP!");

            sender.Close();
            session.Close();
            connection.Close();
        }
    }
}
```

In Receiver project, open Program.cs file and type in the following code.
```
using System;
using Amqp;

namespace Receiver
{
    class Program
    {
        static void Main(string[] args)
        {
            Address address = new Address("amqp://guest:guest@localhost:5672");
            Connection connection = new Connection(address);
            Session session = new Session(connection);
            ReceiverLink receiver = new ReceiverLink(session, "receiver-link", "q1");

            Console.WriteLine("Receiver connected to broker.");
            Message message = receiver.Receive(-1);
            Console.WriteLine("Received " + message.GetBody<string>());
            receiver.Accept(message);

            receiver.Close();
            session.Close();
            connection.Close();
        }
    }
}
```

Buid both projects.

## Run

Open two console windows. Run Receiver.exe from one window. It should output the following.  
`Receiver connected to broker.`

Run Sender.exe from the other window. It should output the following.  
`Sent Hello AMQP!`

The Receiver window should output the following right after the Sender completes.  
`Received Hello AMQP!`


That's it! You just sent an AMQP message to the broker and received it back. Happy messaging!