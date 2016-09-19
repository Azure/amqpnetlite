The library is installed to the application projects through the NuGet packages.  

## NuGet Packages

The following NuGet packages are available.

* __AMQPNetLite__ - the package containing the targets of all supported platforms.
* __AMQPNetMicro__ - the package containing a compact version for NETMF. To reduce
storage and memory footprints, this version has its own APIs and implementation
that are specifically designed for memory and space constrained devices.
* .Net Core packages - the packages for any netstandard1.3 compliant platforms.
  * __AMQPNetLite.Core__ - the core protocol implementation and the client/listener
APIs.
  * __AMQPNetLite.Serialization__ - the AMQP Serializer that serializes custom
types using the standard AMQP type system.
  * __AMQPNetLite.WebSockets__ - the WebSocket transport for the client.

## Installation

To reference the library, install the corresponding NuGet package into the
projects you are developing.

### Visual Studio

Open Manage NuGet Packages window. Search for the package mentioned above for
the platform of your application. Install the package.

Alternatively, open Package Manager Console and execute the following command.
```
Install-Package AmqpNetLite -ProjectName YourProjectName
```

### .Net Core cli

If you develop with other editors and build with the cli tool chain, add the
following to your project.json file.

```
  "dependencies": {
    "AMQPNetLite.Core": "2.0.0",
    "Other.Dependency": "1.0.0"
  }
```