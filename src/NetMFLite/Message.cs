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

    public class Message
    {
        public Map MessageAnnotations { get; set; }

        public Map ApplicationProperties { get; set; }

        public object Body { get; set; }

        internal uint deliveryId;
        internal bool settled;

        internal void Encode(ByteBuffer buffer)
        {
            if (this.MessageAnnotations != null)
            {
                Encoder.WriteObject(buffer, new DescribedValue(0x72ul, this.MessageAnnotations));
            }

            if (this.ApplicationProperties != null)
            {
                Encoder.WriteObject(buffer, new DescribedValue(0x74ul, this.ApplicationProperties));
            }

            if (this.Body != null)
            {
                Encoder.WriteObject(buffer, new DescribedValue(0x77ul, this.Body));
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