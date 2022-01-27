Listener APIs can be used to build applications such as P2P service, router, broker and more. Depending on how much control of the protocol required by the application, different listener APIs can be used.

## ContainerHost
ContainerHost is the easiest way to start an AMQP listener. Multiple endpoints can be specified in a single host. The host internally listens on all transport endpoints for incoming connections.

The application is required to register at least one of the following processors to process AMQP events. Multiple message/request processors can be registered as long as their addresses are different. At most one link processor can be registered.

When the container host receives an attach performative from the remote peer,
(1) if an address resolver is set, the host first calls the resolver to translate the address from the incoming attach request.
The resolver allows the application to implement various logic for mapping a peer specified address to a listener address, which
is used to register a processor. Common scenarios are message processor to route messages to different destinations and message
source to serve messages from multiple nodes.
(2) if a message/request processor is found at the registered address, the host creates a link endpoint for that address and all received messages are routed to that processor.  
(3) otherwise, if a link processor is registered, the attach request is routed to the processor.  
(4) otherwise, the attach is rejected with "amqp:not-found" error.

Protocol behavior can be configured through the Listeners properties of the container host.

### IMessageProcessor
For one-way communication from client to listener, application implements this interface to process incoming messages. Application finishes message processing by completing the given MessageContext object, which sends the acknowledgement (AMQP disposition) to the remote peer if required.

### IRequestProcessor
For two-way bidirectional request/response communication, application implements this interface to process incoming request messages and send back response messages. Application finishes request processing by completing the given RequestContext object with a response message. The container host manages all the request/response links, the correlation between request/response messages and routing the response message to the right response link.

### ILinkProcessor
Link processor can be implemented in the following scenarios:  
(1) application needs extra validation or security check for the link attachment, or to perform link specific initialization and clean up.  
(2) sending messages from the listener to the client.  
(3) handling attach requests on addresses which are not known beforehand.  

Link processor finishes attach processing by completing the given AttachContext with a LinkEndpoint implementation. The subsequent message, flow and disposition contexts are routed to the endpoint for processing.

## ConnectionListener and IContainer
Application may need access to the lower level of the protocol, or require more capabilities than the container host can provide. In such cases, it can manage connections and container links with these interfaces.
It is more flexible and gives more control but requires more implementation.