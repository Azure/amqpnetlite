//  ------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation
//  All rights reserved. 
//  
//  Licensed under the Apache License, Version 2.0 (the ""License""); you may not use this 
//  file except in compliance with the License. You may obtain a copy of the License at 
//  http://www.apache.org/licenses/LICENSE-2.0  
//  
//  THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
//  EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
//  CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR 
//  NON-INFRINGEMENT. 
// 
//  See the Apache Version 2.0 License for specific language governing permissions and 
//  limitations under the License.
//  ------------------------------------------------------------------------------------

namespace ServiceBus.Scenarios
{
    using System;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using Amqp;
    using Amqp.Framing;
    using Amqp.Sasl;

    class CbsAsyncExample : Example
    {
        public override void Run()
        {
            this.RunSampleAsync().GetAwaiter().GetResult();
        }

        async Task RunSampleAsync()
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.SASL.Profile = SaslProfile.Anonymous;

            Trace.WriteLine(TraceLevel.Information, "Establishing a connection...");
            Address address = new Address(this.Namespace, 5671, null, null, "/", "amqps");
            var connection = await factory.CreateAsync(address);

            // before any operation can be performed, a token must be put to the $cbs node
            Trace.WriteLine(TraceLevel.Information, "Putting a token to the $cbs node...");
            await PutTokenAsync(connection);

            Trace.WriteLine(TraceLevel.Information, "Sending a message...");
            var session = new Session(connection);
            var sender = new SenderLink(session, "ServiceBus.Cbs:sender-link", this.Entity);
            await sender.SendAsync(new Message("test"));
            await sender.CloseAsync();

            Trace.WriteLine(TraceLevel.Information, "Receiving the message back...");
            var receiver = new ReceiverLink(session, "ServiceBus.Cbs:receiver-link", this.Entity);
            var message = await receiver.ReceiveAsync();
            receiver.Accept(message);
            await receiver.CloseAsync();

            Trace.WriteLine(TraceLevel.Information, "Closing the connection...");
            await session.CloseAsync();
            await connection.CloseAsync();
        }

        async Task PutTokenAsync(Connection connection)
        {
            var session = new Session(connection);

            string cbsClientAddress = "cbs-client-reply-to";
            var cbsSender = new SenderLink(session, "cbs-sender", "$cbs");
            var receiverAttach = new Attach()
            {
                Source = new Source() { Address = "$cbs" },
                Target = new Target() { Address = cbsClientAddress }
            };
            var cbsReceiver = new ReceiverLink(session, "cbs-receiver", receiverAttach, null);
            var sasToken = GetSASToken(this.KeyName, this.KeyValue, string.Format("http://{0}/{1}", this.Namespace, this.Entity), TimeSpan.FromMinutes(20));
            Trace.WriteLine(TraceLevel.Information, " sas token: {0}", sasToken);

            // construct the put-token message
            var request = new Message(sasToken);
            request.Properties = new Properties();
            request.Properties.MessageId = "1";
            request.Properties.ReplyTo = cbsClientAddress;
            request.ApplicationProperties = new ApplicationProperties();
            request.ApplicationProperties["operation"] = "put-token";
            request.ApplicationProperties["type"] = "servicebus.windows.net:sastoken";
            request.ApplicationProperties["name"] = string.Format("amqp://{0}/{1}", this.Namespace, this.Entity);
            await cbsSender.SendAsync(request);
            Trace.WriteLine(TraceLevel.Information, " request: {0}", request.Properties);
            Trace.WriteLine(TraceLevel.Information, " request: {0}", request.ApplicationProperties);

            // receive the response
            var response = await cbsReceiver.ReceiveAsync();
            if (response == null || response.Properties == null || response.ApplicationProperties == null)
            {
                throw new Exception("invalid response received");
            }

            // validate message properties and status code.
            Trace.WriteLine(TraceLevel.Information, " response: {0}", response.Properties);
            Trace.WriteLine(TraceLevel.Information, " response: {0}", response.ApplicationProperties);
            int statusCode = (int)response.ApplicationProperties["status-code"];
            if (statusCode != (int)HttpStatusCode.Accepted && statusCode != (int)HttpStatusCode.OK)
            {
                throw new Exception("put-token message was not accepted. Error code: " + statusCode);
            }

            // the sender/receiver may be kept open for refreshing tokens
            await cbsSender.CloseAsync();
            await cbsReceiver.CloseAsync();
            await session.CloseAsync();
        }

        static string GetSASToken(string keyName, string keyValue, string requestUri, TimeSpan ttl)
        {
            // http://msdn.microsoft.com/en-us/library/azure/dn170477.aspx
            // The canonical Uri scheme is http because the token is not amqp specific.
            // The request Uri must be a full Uri including the scheme part.
            // Signature is computed from joined encoded request Uri string and expiry string
            string expiry = ((long)((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)) + ttl).TotalSeconds).ToString();
            string encodedUri = HttpUtility.UrlEncode(requestUri);

            string sig;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(keyValue)))
            {
                sig = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(encodedUri + "\n" + expiry)));
            }

            return string.Format(
                "SharedAccessSignature sig={0}&se={1}&skn={2}&sr={3}",
                HttpUtility.UrlEncode(sig),
                HttpUtility.UrlEncode(expiry),
                HttpUtility.UrlEncode(keyName),
                encodedUri);
        }
    }
}
