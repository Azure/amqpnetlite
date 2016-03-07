# amqpnetlite
AMQP.Net Lite is a lightweight AMQP 1.0 client library for the .Net Micro Framework, .Net Compact Framework, .Net Framework, and Windows Runtime platforms.

## Features
* Full control of AMQP 1.0 protocol behavior 
* Peer-to-peer and brokered messaging 
* Secure communication via TLS and SASL 
* Sync and async API support 
* Listener APIs to enable wide range of listener applications, including brokers, routers, proxies, and more. 
* A lightweight messaging library that runs on all popular .NET and Windows Runtime platforms

## Supported Platforms
|            | net45 | net40 | net35 | netmf | netcf | win8/wp8 | netcore451/uwp | dnxcore50<sup>4</sup> |
|------------|:-----:|:-----:|:-----:|:-----:|:-----:|:--------:|:----------:|:----------:|
| TLS        |  +    |   +   |   +   |   +<sup>1</sup>  |   +   |    +     |     +      |     +      |
| SASL<sup>2</sup>      |  +    |   +   |   +   |   +   |   +   |    +     |     +      |     +      |
| AMQP Core  |  +    |   +   |   +   |   +   |   +   |    +     |     +      |     +      |
| Txn        |  +    |   +   |       |       |       |          |            |            |
| Async API  |  +    |   +<sup>3</sup>   |       |       |       |    +     |     +      |     +      |
| Listener   |  +    |   +   |       |       |       |          |            |     +      |
| Serializer |  +    |   +   |   +   |       |       |          |            |     +      |
| WebSockets |  +    |       |       |       |       |          |            |            |

1. requires a TLS-capable device.
2. only SASL PLAIN and SASL EXTERNAL are currently supported.
3. requires Microsoft.Bcl.Async.
4. experimenting (not available in the NuGet package)

## Tested Platforms
* .Net Framework (up to 4.5) 
* .NET Micro Framework 4.2 and 4.3 
* .NET Compact Framework 3.9
* Windows Phone 8 and 8.1
* Mono on Linux (requires v4.2.1 and up. Only the client APIs are verified and state of the listener APIs is unknown.)

## Getting Started
* Prerequisites:
  * Visual Studio 2013 (e.g. Community Edition)
  * NETMF SDK (4.2-4.4) and Visual Studio project system
  * Application Builder for Windows Embedded Compact 2013
  * NuGet tools if you want to build the NuGet package
* Build: from inside Visual Studio or by running build.cmd script
* Examples: refer to the examples for common scenarios.

## References
For more information about the Azure Service Bus and AMQP, refer to:
* Azure Service Bus:  http://msdn.microsoft.com/en-us/library/ee732537.aspx. 
* Azure Service Bus and AMQP:  http://msdn.microsoft.com/en-us/library/jj841071.aspx 
* Azure Service Bus Event Hub:  http://azure.microsoft.com/en-us/services/event-hubs/ 
* AMQP:  http://docs.oasis-open.org/amqp/core/v1.0/os/amqp-core-overview-v1.0-os.html

