# AMQP.Net Lite

AMQP.Net Lite is a lightweight AMQP 1.0 library for the .Net Framework, .Net Core, Windows Runtime platforms, .Net Micro Framework, .NET nanoFramework and Mono. The library includes both a client and listener to enable peer to peer and broker based messaging.  
[Documentation](http://azure.github.io/amqpnetlite/)

[![Build status](https://ci.appveyor.com/api/projects/status/dph11pp7doubyw7t/branch/master?svg=true)](https://ci.appveyor.com/project/xinchen10/amqpnetlite/branch/master)

|NuGet Package|Status|
|------|-------------|
|AMQPNetLite (main package)|[![Version](https://img.shields.io/nuget/v/AMQPNetLite.svg) ![Downloads](https://img.shields.io/nuget/dt/AMQPNetLite.svg)](https://www.nuget.org/packages/AMQPNetLite/)|
|AMQPNetLite.Core (.Net Core)|[![Version](https://img.shields.io/nuget/v/AMQPNetLite.Core.svg) ![Downloads](https://img.shields.io/nuget/dt/AMQPNetLite.Core.svg)](https://www.nuget.org/packages/AMQPNetLite.Core/)|
|AMQPNetLite.Serialization (.Net Core)|[![Version](https://img.shields.io/nuget/v/AMQPNetLite.Serialization.svg) ![Downloads](https://img.shields.io/nuget/dt/AMQPNetLite.Serialization.svg)](https://www.nuget.org/packages/AMQPNetLite.Serialization/)|
|AMQPNetLite.WebSockets (.Net Core)|[![Version](https://img.shields.io/nuget/v/AMQPNetLite.WebSockets.svg) ![Downloads](https://img.shields.io/nuget/dt/AMQPNetLite.WebSockets.svg)](https://www.nuget.org/packages/AMQPNetLite.WebSockets/)|
|AMQPNetLite.NetMF (NETMF)|[![Version](https://img.shields.io/nuget/v/AMQPNetLite.NetMF.svg) ![Downloads](https://img.shields.io/nuget/dt/AMQPNetLite.NetMF.svg)](https://www.nuget.org/packages/AMQPNetLite.NetMF/)|
|AMQPNetMicro (NETMF)|[![Version](https://img.shields.io/nuget/v/AMQPNetMicro.svg) ![Downloads](https://img.shields.io/nuget/dt/AMQPNetMicro.svg)](https://www.nuget.org/packages/AMQPNetMicro/)|
|AMQPNetLite.nanoFramework (nanoFramework)|[![Version](https://img.shields.io/nuget/v/AMQPNetLite.nanoFramework.svg) ![Downloads](https://img.shields.io/nuget/dt/AMQPNetLite.nanoFramework.svg)](https://www.nuget.org/packages/AMQPNetLite.nanoFramework/)|
|AMQPNetMicro.nanoFramework (nanoFramework)|[![Version](https://img.shields.io/nuget/v/AMQPNetMicro.nanoFramework.svg) ![Downloads](https://img.shields.io/nuget/dt/AMQPNetMicro.nanoFramework.svg)](https://www.nuget.org/packages/AMQPNetMicro.nanoFramework/)|


## Features
* Full control of AMQP 1.0 protocol behavior.
* Peer-to-peer and brokered messaging.
* Secure communication via TLS and SASL.
* Extensible transport providers.
* Sync and async API support.
* Listener APIs to enable wide range of listener applications, including brokers, routers, proxies, and more.
* A lightweight messaging library that runs on all popular .NET and Windows Runtime platforms.

The following table shows what features are supported on each platform/framework.

|        | TLS | SASL<sup>2</sup> | Txn | Task | Serializer | Listener | WebSockets | BufferPooling |
|:-------|:---:|:----------------:|:---:|:----:|:----------:|:--------:|:----------:|:-------------:|
|net45   |+|+|+|+|+|+|+|+|
|net40   |+|+|+|+<sup>3</sup>|+|+| |+|
|net35   |+|+| | |+| | | |
|netmf   |+<sup>1</sup>|+| | | | | | |
|nanoFramework|+|+| | | | | | |
|uap10|+|+| |+| | | | |
|netcore451|+|+| |+| | | | |
|wpa81   |+|+| |+| | | | |
|win8/wp8|+|+| |+| | | | |
|netstandard1.3<sup>4</sup>|+|+| |+|+|+|+|+|
|mono/Xamarin<sup>5</sup>|+|+| |+|+|+|+|+|

1. requires a TLS-capable device.
2. only SASL PLAIN, EXTERNAL, and ANONYMOUS are currently supported.
3. requires Microsoft.Bcl.Async.
4. has 3 packages. Supports WebSocket client but not listener.
5. projects targeting Mono/Xamarin should be able to consume the netstandard1.3 library.

## Tested Platforms
* .Net Framework 3.5, 4.0 and 4.5+.
* .NET Micro Framework 4.2, 4.3, 4.4.
* .NET nanoFramework 1.0.
* .NET Compact Framework 3.9.
* Windows Phone 8 and 8.1.
* Windows Store 8 and 8.1. Universal Windows App 10.
* .Net Core 1.0 on Windows 10 and Ubuntu 14.04.
* Mono on Linux (requires v4.2.1 and up. Only the client APIs are verified and state of the listener APIs is unknown).

## Getting Started
* [Quick Start](docs/articles/building_application.md) Build applications from simple to complex.
* [Examples](https://github.com/Azure/amqpnetlite/tree/master/Examples) Please take a minute to look at the examples.
* [.Net Core](https://github.com/Azure/amqpnetlite/tree/master/dotnet) If you are looking for information about using amqpnetlite on .Net Core (coreclr, dnxcore50, etc.), your can find the code and a Hello AMQP! example here.
* [Interested in the code?](docs/articles/working_with_code.md) Clone and build the projects.

## Contributing
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

If you would like to become an contributor to this project please follow the instructions provided in [Microsoft Azure Projects Contribution Guidelines](http://azure.github.io/guidelines/).

## References
For more information about the Azure Service Bus and AMQP, refer to:
* Azure Service Bus:  http://msdn.microsoft.com/en-us/library/ee732537.aspx. 
* Azure Service Bus and AMQP:  http://msdn.microsoft.com/en-us/library/jj841071.aspx 
* Azure Service Bus Event Hub:  http://azure.microsoft.com/en-us/services/event-hubs/ 
* AMQP:  http://docs.oasis-open.org/amqp/core/v1.0/os/amqp-core-overview-v1.0-os.html

