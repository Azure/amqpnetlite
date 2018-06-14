# Prerequisites
* Visual Studio 2013. [Community Edition](https://www.visualstudio.com/en-us/news/vs2013-community-vs.aspx) works.
* NETMF SDK (4.2 and 4.3) and Visual Studio project system. You can build fro [sources](https://github.com/NETMF/netmf-interpreter) or download them from the old [netmf web site](https://netmf.codeplex.com).
* NuGet tools if you want to build the NuGet package. 

# Build the projects
* Build with Visual Studio. Open amqp.sln in Visual Studio. This solution contains both source and test projects for all supported platforms. If the SDK of a particular platform is not present, the project(s) will fail to load. You can either install the required SDK or remove the project(s) from the solution.
* Build from command prompt. Run the build.cmd script to build the solution. If you need to build the NuGet packet, please [install NuGet](http://docs.nuget.org/consume/installing-nuget) or download NuGet.exe directly and save it under ".\build\tools\" directory.

# Run the tests
* Most of the tests require a broker to run. You need a broker preconfigured with a queue (or an broker specific entity that maps to an AMQP node named "q1"). Update the address (hostname, port, etc) to match the broker config before you run the tests.
* The solution has a test broker which can be used to run tests. It can be started by running the following command. Note that the value of the "/cert" option is the subject name or the thumbprint of the service certificate that is already installed on the machine.
`TestAmqpBroker.exe amqp://localhost:5672 amqps://localhost:5671 ws://localhost:80 /creds:guest:guest /cert:localhost`
* NETMF tests are in project Test.Amqp.NetMF42/43. It is a NETMF application that runs in the emulator or a real device. It executes all methods whose names begin with "TestMethod_". 

# Start building applications
* First take a look at the example projects under the Examples directory. The examples are working code against the Azure Service Bus service or other AMQP 1.0 compliant brokers.
* The API documentation today is in the source code. We will create a wiki page for that soon.
* You may also review the code of a few test cases. Just note that test cases are organized by functionality coverage so it may not be easy to find what you need.
