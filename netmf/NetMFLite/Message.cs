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
    using Amqp.Types;

    /// <summary>
    /// Defines an AMQP message.
    /// </summary>
    public class Message
    {
        // List of the fields defined in Properties
        // Most commonly used properties have getter/setter
        // To access others, user can access the Properties list
        List properties;

        /// <summary>
        /// Gets or sets the message-id field.
        /// </summary>
        public string MessageId
        {
            get { return (string)this.Properties[0]; }
            set { this.Properties[0] = value; }
        }

        /// <summary>
        /// Gets or sets the to field.
        /// </summary>
        public string To
        {
            get { return (string)this.Properties[2]; }
            set { this.Properties[2] = value; }
        }

        /// <summary>
        /// Gets or sets the subject field.
        /// </summary>
        public string Subject
        {
            get { return (string)this.Properties[3]; }
            set { this.Properties[3] = value; }
        }

        /// <summary>
        /// Gets or sets the correlation-id field.
        /// </summary>
        public string CorrelationId
        {
            get { return (string)this.Properties[5]; }
            set { this.Properties[5] = value; }
        }

        /// <summary>
        /// Gets or sets the message-annotations section.
        /// </summary>
        public Map MessageAnnotations
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        public List Properties
        {
            get
            {
                if (this.properties == null)
                {
                    this.properties = new List() { null, null, null, null, null, null, null, null, null, null, null, null, null };
                }

                return this.properties;
            }
        }

        /// <summary>
        /// Gets or sets the application-properties section.
        /// </summary>
        public Map ApplicationProperties
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        public object Body { get; set; }

        internal uint deliveryId;
        internal bool settled;

        internal void Encode(ByteBuffer buffer)
        {
            if (this.MessageAnnotations != null)
            {
                Encoder.WriteObject(buffer, new DescribedValue(0x72ul, this.MessageAnnotations));
            }

            if (this.Properties != null)
            {
                Encoder.WriteObject(buffer, new DescribedValue(0x73ul, this.Properties));
            }

            if (this.ApplicationProperties != null)
            {
                Encoder.WriteObject(buffer, new DescribedValue(0x74ul, this.ApplicationProperties));
            }

            if (this.Body != null)
            {
                if (this.Body is byte[])
                {
                    Encoder.WriteObject(buffer, new DescribedValue(0x75ul, this.Body));
                }
                else
                {
                    Encoder.WriteObject(buffer, new DescribedValue(0x77ul, this.Body));
                }
            }
        }

        internal static Message Decode(ByteBuffer buffer)
        {
            Message message = new Message();
            while (buffer.Length > 0)
            {
                var section = (DescribedValue)Encoder.ReadObject(buffer);
                if (section.Descriptor.Equals(0x72ul))
                {
                    message.MessageAnnotations = (Map)section.Value;
                }
                else if (section.Descriptor.Equals(0x73ul))
                {
                    List list = (List)section.Value;
                    for (int i = list.Count; i < 13; i++)
                    {
                        list.Add(null);
                    }
                    
                    message.properties = list;
                }
                else if (section.Descriptor.Equals(0x74ul))
                {
                    message.ApplicationProperties = (Map)section.Value;
                }
                else if (section.Descriptor.Equals(0x75ul) ||
                    section.Descriptor.Equals(0x76ul) ||
                    section.Descriptor.Equals(0x77ul))
                {
                    message.Body = section.Value;
                }
            }

            return message;
        }
    }
}