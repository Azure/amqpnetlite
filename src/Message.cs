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

namespace Amqp
{
    using Amqp.Framing;
    using Amqp.Types;
    using System;

    public class Message
    {
        public Header Header;
        public DeliveryAnnotations DeliveryAnnotations;
        public MessageAnnotations MessageAnnotations;
        public Properties Properties;
        public ApplicationProperties ApplicationProperties;
        public Footer Footer;
        public DescribedValue Body; // support single Data or AmqpSequence section only

        public Message()
        {
        }

        public Message(object body)
        {
            this.Body = new AmqpValue() { Value = body };
        }

        public AmqpValue ValueBody
        {
            get { return (AmqpValue)this.Body; }
        }

        public Data DataBody
        {
            get { return (Data)this.Body; }
        }

        public AmqpSequence SequenceBody
        {
            get { return (AmqpSequence)this.Body; }
        }

        internal Delivery Delivery
        {
            get;
            set;
        }

        public ByteBuffer Encode()
        {
            ByteBuffer buffer = new ByteBuffer(128, true);
            if (this.Header != null) Codec.Encode(buffer, this.Header);
            if (this.DeliveryAnnotations != null) Codec.Encode(buffer, this.DeliveryAnnotations);
            if (this.MessageAnnotations != null) Codec.Encode(buffer, this.MessageAnnotations);
            if (this.Properties != null)  Codec.Encode(buffer, this.Properties);
            if (this.ApplicationProperties != null) Codec.Encode(buffer, this.ApplicationProperties);
            if (this.Body != null) Codec.Encode(buffer, this.Body);
            if (this.Footer != null) Codec.Encode(buffer, this.Footer);
            return buffer;
        }

        public Message Decode(ByteBuffer buffer)
        {
            while (buffer.Length > 0)
            {
                Described described = Codec.Decode(buffer);
                if (described.Descriptor.Code == Codec.Header.Code)
                {
                    this.Header = (Header)described;
                }
                else if (described.Descriptor.Code == Codec.DeliveryAnnotations.Code)
                {
                    this.DeliveryAnnotations = (DeliveryAnnotations)described;
                }
                else if (described.Descriptor.Code == Codec.MessageAnnotations.Code)
                {
                    this.MessageAnnotations = (MessageAnnotations)described;
                }
                else if (described.Descriptor.Code == Codec.Properties.Code)
                {
                    this.Properties = (Properties)described;
                }
                else if (described.Descriptor.Code == Codec.ApplicationProperties.Code)
                {
                    this.ApplicationProperties = (ApplicationProperties)described;
                }
                else if (described.Descriptor.Code == Codec.AmqpValue.Code ||
                    described.Descriptor.Code == Codec.Data.Code ||
                    described.Descriptor.Code == Codec.AmqpSequence.Code)
                {
                    this.Body = (DescribedValue)described;
                }
                else if (described.Descriptor.Code == Codec.Footer.Code)
                {
                    this.Footer = (Footer)described;
                }
                else
                {
                    throw new AmqpException(ErrorCode.FramingError,
                        Fx.Format(SRAmqp.AmqpUnknownDescriptor, described.Descriptor));
                }
            }

            return this;
        }
    }
}