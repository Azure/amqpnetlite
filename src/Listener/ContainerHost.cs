﻿//  ------------------------------------------------------------------------------------
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
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Amqp.Framing;
    using Amqp.Types;

    /// <summary>
    /// The ContainerHost class hosts an AMQP container where connection
    /// listeners can be created to accept client requests.
    /// </summary>
    /// <remarks>
    /// A container has one or more connection endpoints where transport
    /// listeners are created.
    /// 
    /// Message-level Processing
    ///  * IMessageProcessor: message processors can be registered to accept
    ///    incoming messages (one-way incoming).
    ///  * IMessageSource: message sources can be registered to handle
    ///    receive requests (one-way outgoing).
    ///  * IRequestProcessor: request processors can be registered to
    ///    process request messages and send back response (two-way).
    /// 
    /// Link-level Processing
    /// Message level processing only deals with incoming and outgoing messages
    /// without worrying about the links.
    /// Link processors (ILinkProcessor) can be registered to process received
    /// attach performatives. This is useful when the application needs to
    /// participate in link attach/detach for extra resource allocation/cleanup,
    /// or perform additional validation and security enforcement at the link level.
    /// Link processors create link endpoints which can be either message sink
    /// or message source.
    ///
    /// Upon receiving an attach performative, the registered message level
    /// processors (IMessageProcessor, IMessageSource, IRequestProcessor) are
    /// checked first. If a processor matches the address on the received attach
    /// performative, a link is automatically created and the send/receive requests
    /// will be rounted to the associated processor.
    /// Otherwise, the registered link processor, if any, is invoked to create a
    /// LinkEndpoint, where subsequent send/receive requests will be routed.
    /// When none is found, the link is detached with error "amqp:not-found".
    /// </remarks>
    public class ContainerHost : IContainer
    {
        readonly string containerId;
        readonly ConnectionListener[] listeners;
        readonly LinkCollection linkCollection;
        readonly ClosedCallback onLinkClosed;
        readonly Dictionary<string, MessageProcessor> messageProcessors;
        readonly Dictionary<string, RequestProcessor> requestProcessors;
        readonly Dictionary<string, MessageSource> messageSources;
        ILinkProcessor linkProcessor;

        /// <summary>
        /// Initializes a container host object with multiple address.
        /// </summary>
        /// <param name="addressList">The list of listen addresses.</param>
        /// <remarks>
        /// Transport and protocol settings (TCP, SSL, SASL and AMQP) can be
        /// set on the listeners of the host after it is created.
        /// </remarks>
        public ContainerHost(IList<string> addressList)
        {
            this.containerId = string.Join("-", this.GetType().Name, Guid.NewGuid().ToString("N"));
            this.linkCollection = new LinkCollection(this.containerId);
            this.onLinkClosed = this.OnLinkClosed;
            this.messageProcessors = new Dictionary<string, MessageProcessor>(StringComparer.OrdinalIgnoreCase);
            this.requestProcessors = new Dictionary<string, RequestProcessor>(StringComparer.OrdinalIgnoreCase);
            this.messageSources = new Dictionary<string, MessageSource>(StringComparer.OrdinalIgnoreCase);
            this.listeners = new ConnectionListener[addressList.Count];
            for (int i = 0; i < addressList.Count; i++)
            {
                this.listeners[i] = new ConnectionListener(addressList[i], this);
                this.listeners[i].AMQP.ContainerId = this.containerId;
            }
        }

        /// <summary>
        /// Initializes a container host object with one address.
        /// </summary>
        /// <param name="addressUri">The address Uri. Only the scheme, host and port parts are used.
        /// Supported schema are "amqp", "amqps", "ws" and "wss".</param>
        public ContainerHost(Uri addressUri)
            : this(new string[] { addressUri.AbsoluteUri })
        {
        }

        /// <summary>
        /// Initializes a container host object with multiple address.
        /// </summary>
        /// <param name="addressUriList">The list of listen addresses.</param>
        /// <param name="certificate">The service certificate for TLS.</param>
        /// <param name="userInfo">The credentials required by SASL PLAIN authentication. It is of
        /// form "user:password" (parts are URL encoded).</param>
        public ContainerHost(IList<Uri> addressUriList, X509Certificate2 certificate, string userInfo)
            : this(As(addressUriList, u => u.AbsoluteUri))
        {
            for (int i = 0; i < this.listeners.Length; i++)
            {
                this.listeners[i].SSL.Certificate = certificate;
                this.listeners[i].SetUserInfo(userInfo);
            }
        }

        /// <summary>
        /// Gets a list of connection listeners in this container.
        /// </summary>
        public IList<ConnectionListener> Listeners
        {
            get { return this.listeners; }
        }

        /// <summary>
        /// Opens the container host object.
        /// </summary>
        public void Open()
        {
            foreach (var listener in this.listeners)
            {
                listener.Open();
            }
        }

        /// <summary>
        /// Closes the container host object.
        /// </summary>
        public void Close()
        {
            foreach (var listener in this.listeners)
            {
                try
                {
                    listener.Close();
                }
                catch (Exception exception)
                {
                    Trace.WriteLine(TraceLevel.Error, exception.ToString());
                }
            }
        }

        /// <summary>
        /// Registers a link process to handle received attach performatives.
        /// </summary>
        /// <param name="linkProcessor">The link processor to be registered.</param>
        public void RegisterLinkProcessor(ILinkProcessor linkProcessor)
        {
            if (this.linkProcessor != null)
            {
                throw new AmqpException(ErrorCode.NotAllowed, this.linkProcessor.GetType().Name + " already registered");
            }

            this.linkProcessor = linkProcessor;
        }

        /// <summary>
        /// Registers a message processor to accept incoming messages from the specified address.
        /// When it is called, the container creates a node where the client can attach.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="messageProcessor">The message processor to be registered.</param>
        public void RegisterMessageProcessor(string address, IMessageProcessor messageProcessor)
        {
            ThrowIfExists(address, this.requestProcessors);
            AddProcessor(this.messageProcessors, address, new MessageProcessor(messageProcessor));
        }

        /// <summary>
        /// Registers a message source at the specified address where client receives messages.
        /// When it is called, the container creates a node where the client can attach.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="messageSource">The message source to be registered.</param>
        public void RegisterMessageSource(string address, IMessageSource messageSource)
        {
            ThrowIfExists(address, this.requestProcessors);
            AddProcessor(this.messageSources, address, new MessageSource(messageSource));
        }

        /// <summary>
        /// Registers a request processor from the specified address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="requestProcessor">The request processor to be registered.</param>
        /// <remarks>
        /// Client must create a pair of links (sending and receiving) at the address. The
        /// source.address on the sending link should contain an unique address in the client
        /// and it should be specified in target.address on the receiving link.
        /// </remarks>
        public void RegisterRequestProcessor(string address, IRequestProcessor requestProcessor)
        {
            ThrowIfExists(address, this.messageProcessors);
            ThrowIfExists(address, this.messageSources);
            AddProcessor(this.requestProcessors, address, new RequestProcessor(requestProcessor));
        }

        /// <summary>
        /// Unregisters a message processor at the specified address.
        /// </summary>
        /// <param name="address">The address.</param>
        public void UnregisterMessageProcessor(string address)
        {
            RemoveProcessor(this.messageProcessors, address);
        }

        /// <summary>
        /// Unregisters a message source at the specified address.
        /// </summary>
        /// <param name="address">The address.</param>
        public void UnregisterMessageSource(string address)
        {
            RemoveProcessor(this.messageSources, address);
        }

        /// <summary>
        /// Unregisters a request processor at the specified address.
        /// </summary>
        /// <param name="address">The address.</param>
        public void UnregisterRequestProcessor(string address)
        {
            RemoveProcessor(this.requestProcessors, address);
        }

        static IList<TOut> As<TIn, TOut>(IList<TIn> inList, Func<TIn, TOut> func)
        {
            TOut[] outList = new TOut[inList.Count];
            for (int i = 0; i < outList.Length; i++)
            {
                outList[i] = func(inList[i]);
            }

            return outList;
        }

        static void ThrowIfExists<T>(string address, Dictionary<string, T> processors)
        {
            if (processors.ContainsKey(address))
            {
                throw new AmqpException(ErrorCode.NotAllowed, typeof(T).Name + " processor has been registered at address " + address);
            }
        }

        static void AddProcessor<T>(Dictionary<string, T> processors, string address, T processor)
        {
            lock (processors)
            {
                if (processors.ContainsKey(address))
                {
                    throw new AmqpException(ErrorCode.NotAllowed, typeof(T).Name + " already registered");
                }

                processors[address] = processor;
            }
        }

        static void RemoveProcessor<T>(Dictionary<string, T> processors, string address)
        {
            lock (processors)
            {
                T processor;
                if (processors.TryGetValue(address, out processor))
                {
                    processors.Remove(address);
                    if (processor is IDisposable)
                    {
                        ((IDisposable)processor).Dispose();
                    }
                }
            }
        }

        static bool TryGetProcessor<T>(Dictionary<string, T> processors, string address, out T processor)
        {
            lock (processors)
            {
                return processors.TryGetValue(address, out processor);
            }
        }

        X509Certificate2 IContainer.ServiceCertificate
        {
            // listener should get it from SslSettings
            get { throw new InvalidOperationException(); }
        }

        Message IContainer.CreateMessage(ByteBuffer buffer)
        {
            return Message.Decode(buffer);
        }

        Link IContainer.CreateLink(ListenerConnection connection, ListenerSession session, Attach attach)
        {
            ListenerLink link = new ListenerLink(session, attach);
            link.SafeAddClosed(this.onLinkClosed);
            return link;
        }

        bool IContainer.AttachLink(ListenerConnection connection, ListenerSession session, Link link, Attach attach)
        {
            var listenerLink = (ListenerLink)link;
            if (!this.linkCollection.TryAdd(listenerLink))
            {
                throw new AmqpException(ErrorCode.Stolen, string.Format("Link '{0}' has been attached already.", attach.LinkName));
            }

            string address = attach.Role ? ((Source)attach.Source).Address : ((Target)attach.Target).Address;
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new AmqpException(ErrorCode.InvalidField, "The address field cannot be empty");
            }

            if (listenerLink.Role)
            {
                MessageProcessor messageProcessor;
                if (TryGetProcessor(this.messageProcessors, address, out messageProcessor))
                {
                    messageProcessor.AddLink(listenerLink, address);
                    return true;
                }
            }
            else
            {
                MessageSource messageSource;
                if (TryGetProcessor(this.messageSources, address, out messageSource))
                {
                    messageSource.AddLink(listenerLink, address);
                    return true;
                }
            }

            RequestProcessor requestProcessor;
            if (TryGetProcessor(this.requestProcessors, address, out requestProcessor))
            {
                requestProcessor.AddLink(listenerLink, address, attach);
                return true;
            }

            if (this.linkProcessor != null)
            {
                this.linkProcessor.Process(new AttachContext(listenerLink, attach));
                return false;
            }

            throw new AmqpException(ErrorCode.NotFound, "No processor was found at " + address);
        }

        void OnLinkClosed(AmqpObject sender, Error error)
        {
            ListenerLink link = (ListenerLink)sender;
            this.linkCollection.Remove(link);
        }

        abstract class Collection<T> : IDisposable
        {
            readonly Dictionary<ListenerLink, T> collection;

            protected Collection()
            {
                this.collection = new Dictionary<ListenerLink, T>();
            }

            public void Add(ListenerLink link, T instance)
            {
                link.SafeAddClosed(this.OnLinkClosed);
                lock (this.collection)
                {
                    this.collection.Add(link, instance);
                }
            }

            void OnLinkClosed(AmqpObject sender, Error error)
            {
                ListenerLink link = (ListenerLink)sender;
                lock (this.collection)
                {
                    this.collection.Remove(link);
                }
            }

            void IDisposable.Dispose()
            {
                lock (this.collection)
                {
                    foreach (var link in this.collection.Keys)
                    {
                        link.Close(0, new Error()
                        {
                            Condition = ErrorCode.DetachForced,
                            Description = "Source was unregistered."
                        });
                    }

                    this.collection.Clear();
                }
            }
        }

        class MessageProcessor : Collection<TargetLinkEndpoint>
        {
            readonly IMessageProcessor messageProcessor;

            public MessageProcessor(IMessageProcessor messageProcessor)
                : base()
            {
                this.messageProcessor = messageProcessor;
            }

            public void AddLink(ListenerLink link, string address)
            {
                TargetLinkEndpoint endpoint = new TargetLinkEndpoint(this.messageProcessor, link);
                link.InitializeLinkEndpoint(endpoint, (uint)this.messageProcessor.Credit);
                this.Add(link, endpoint);
            }
        }

        class MessageSource : Collection<SourceLinkEndpoint>
        {
            readonly IMessageSource messageSource;

            public MessageSource(IMessageSource messageSource)
                : base()
            {
                this.messageSource = messageSource;
            }

            public void AddLink(ListenerLink link, string address)
            {
                SourceLinkEndpoint endpoint = new SourceLinkEndpoint(this.messageSource, link);
                link.InitializeLinkEndpoint(endpoint, 0);
                this.Add(link, endpoint);
            }
        }
        
        class RequestProcessor : IDisposable
        {
            static readonly Action<ListenerLink, Message, DeliveryState, object> dispatchRequest = DispatchRequest;
            readonly IRequestProcessor processor;
            readonly List<ListenerLink> requestLinks;
            readonly Dictionary<string, ListenerLink> responseLinks;

            public RequestProcessor(IRequestProcessor processor)
            {
                this.processor = processor;
                this.requestLinks = new List<ListenerLink>();
                this.responseLinks = new Dictionary<string, ListenerLink>(StringComparer.OrdinalIgnoreCase);
            }

            public IRequestProcessor Processor
            {
                get { return this.processor; }
            }

            public IList<ListenerLink> Links
            {
                get { return this.requestLinks; }
            }

            public void AddLink(ListenerLink link, string address, Attach attach)
            {
                if (!link.Role)
                {
                    string replyTo = ((Target)attach.Target).Address;
                    AddProcessor(this.responseLinks, replyTo, link);
                    link.SettleOnSend = true;
                    link.InitializeSender((c, s) => { }, null, Tuple.Create(this, replyTo));
                    link.SafeAddClosed((s, e) => OnLinkClosed(s, e));
                }
                else
                {
                    link.InitializeReceiver(300, dispatchRequest, this);
                    link.SafeAddClosed((s, e) => OnLinkClosed(s, e));
                    lock (this.requestLinks)
                    {
                        this.requestLinks.Add(link);
                    }
                }
            }

            static void OnLinkClosed(AmqpObject sender, Error error)
            {
                ListenerLink link = (ListenerLink)sender;
                if (!link.Role)
                {
                    var tuple = (Tuple<RequestProcessor, string>)link.State;
                    RemoveProcessor(tuple.Item1.responseLinks, tuple.Item2);
                }
                else
                {
                    var thisPtr = (RequestProcessor)link.State;
                    lock (thisPtr.requestLinks)
                    {
                        thisPtr.requestLinks.Remove(link);
                    }
                }
            }

            static void DispatchRequest(ListenerLink link, Message message, DeliveryState deliveryState, object state)
            {
                RequestProcessor thisPtr = (RequestProcessor)state;

                ListenerLink responseLink = null;
                if (message.Properties != null || message.Properties.ReplyTo != null)
                {
                    thisPtr.responseLinks.TryGetValue(message.Properties.ReplyTo, out responseLink);
                }

                Outcome outcome;
                if (responseLink == null)
                {
                    outcome = new Rejected()
                    {
                        Error = new Error()
                        {
                            Condition = ErrorCode.NotFound,
                            Description = "Not response link was found. Ensure the link is attached or reply-to is set on the request."
                        }
                    };
                }
                else
                {
                    outcome = new Accepted();
                }

                link.DisposeMessage(message, outcome, true);

                RequestContext context = new RequestContext(link, responseLink, message);
                thisPtr.processor.Process(context);
            }

            void IDisposable.Dispose()
            {
                Error error = new Error() { Condition = ErrorCode.DetachForced, Description = "Processor was unregistered." };
                lock (this.requestLinks)
                {
                    for (int i = 0; i < this.requestLinks.Count; i++)
                    {
                        this.requestLinks[i].Close(0, error);
                    }

                    this.requestLinks.Clear();
                }

                lock (this.responseLinks)
                {
                    foreach (var link in this.responseLinks.Values)
                    {
                        link.Close(0, error);
                    }

                    this.responseLinks.Clear();
                }
            }
        }
    }
}
