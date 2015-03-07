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

namespace Amqp.Listener
{
    using Amqp.Framing;

    public interface IMessageProcessor
    {
        void Process(MessageContext messageContext);
    }

    public interface IRequestProcessor
    {
        void Process(RequestContext requestContext);
    }

    public abstract class Context
    {
        protected static Accepted Accepted = new Accepted();

        protected Context(ListenerLink link, Message message)
        {
            this.Link = link;
            this.Message = message;
        }

        public ListenerLink Link
        {
            get;
            private set;
        }

        public Message Message
        {
            get;
            private set;
        }

        protected void Dispose(DeliveryState deliveryState)
        {
            this.Link.DisposeMessage(this.Message, deliveryState, true);
        }
    }

    public class MessageContext : Context
    {
        internal MessageContext(ListenerLink link, Message message)
            : base(link, message)
        {
        }

        public void Complete()
        {
            this.Dispose(Context.Accepted);
        }

        public void Complete(Error error)
        {
            this.Dispose(new Rejected() { Error = error });
        }
    }

    public class RequestContext : Context
    {
        readonly ListenerLink responseLink;

        internal RequestContext(ListenerLink requestLink, ListenerLink responseLink, Message request)
            : base(requestLink, request)
        {
            this.responseLink = responseLink;
        }

        public void Complete(Message response)
        {
            if (response.Properties == null)
            {
                response.Properties = new Properties();
            }

            response.Properties.CorrelationId = this.Message.Properties.MessageId;
            this.responseLink.SendMessage(response, response.Encode());
        }
    }
}
