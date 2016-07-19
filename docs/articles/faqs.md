1. Why does the address parsing fail sometimes if it contains my Service Bus credentials?  
When an address URL is used, the user name and password parts must be URL encoded. (https://www.ietf.org/rfc/rfc1738.txt). Address can also be constructed by providing host, port, optional user info separately, in which case none of these arguments should be URL encoded.

2. Why does the sample code hang in NETMF emulator when running against the Azure Service Bus?  
The Azure Service Bus requires TLS/SSL connection. The SSL driver of the NETMF emulator has an issue in asynchronous read. http://netmf.codeplex.com/workitem/2341

3. Why do I get NotSupportException when running the sample code on my NETMF device?  
Make sure the device is SSL capable.

