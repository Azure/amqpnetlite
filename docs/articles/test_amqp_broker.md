The TestAmqpBroker project is built on top of the .Net version of this library. You can run all MSTest tests in the solution and the NETMF tests against this broker.

## Usage:
```
AmqpTestBroker url [url] [/creds:user:pwd] [/cert:ssl_cert] [/trace:level] [/queues:q1;q2;...]
  url=amqp|amqps://host[:port] (can be multiple)
  creds=username:passwrod
  cert=ssl cert find value (thumbprint or subject)
  trace=level (info, warn, error, frame)
  queues: semicolon separated queue names. If not specified, the broker implicitly
          creates a new node and deletes it when the connection is closed.
```

At least one Uri should be specified. Multiple are allowed (typically one for "amqp" and one for "amqps").

If "amqps" Uri is present, the "cert" option must exist to specify a server certificate for the Tls listener.

If "queues" option is present, the broker is preconfigured with a list of queues. If it does not exist, the broker implicitly creates a queue upon the first attach request
and deletes it when the last connection is closed. This allows running the tests easily without creating or draining the queue. Note that this is different from AMQP dynamic nodes.
You can still create dynamic nodes through the protocol.

When "trace" option is specified, the traces will be printed to the console window.

Example (for running tests):
```
 TestAmqpBroker.exe amqp://localhost:5672 amqps://localhost:5671 ws://localhost:80 /cert:localhost /creds:guest:guest /trace:frame
```
