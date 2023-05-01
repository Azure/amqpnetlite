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

    /// <summary>
    /// Contains the state associated with a message delivery.
    /// </summary>
    public struct MessageDelivery
    {
        readonly Delivery delivery;
        readonly uint messageFormat;

        internal MessageDelivery(Delivery delivery, uint messageFormat)
        {
            this.delivery = delivery;
            this.messageFormat = messageFormat;
        }

        /// <summary>
        /// Returs an empty value for a message that is not delivered yet.
        /// </summary>
        public static MessageDelivery None
        {
            get { return default(MessageDelivery); }
        }

        /// <summary>
        /// Gets the delivery tag.
        /// </summary>
        public byte[] Tag
        {
            get { return this.delivery.Tag; }
        }

        /// <summary>
        /// Gets the delivery state.
        /// </summary>
        public DeliveryState State
        {
            get { return this.delivery.State; }
        }

        /// <summary>
        /// Gets the Link over which the message was transferred, or null if the message is not transferred yet.
        /// </summary>
        public Link Link
        {
            get { return this.delivery.Link; }
        }

        /// <summary>
        /// Gets the format of the message being delivered.
        /// </summary>
        internal uint MessageFormat
        {
            get { return this.messageFormat; }
        }

        internal Delivery Delivery
        {
            get { return this.delivery; }
        }
    };
}