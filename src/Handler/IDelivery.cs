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

namespace Amqp.Handler
{
    using Amqp.Framing;

    /// <summary>
    /// A delivery contains the state for transfering a message.
    /// The properties must not be changed on a received delivery.
    /// between nodes.
    /// </summary>
    public interface IDelivery
    {
        /// <summary>
        /// Gets or sets the delivery tag.
        /// </summary>
        byte[] Tag { get; set; }

        /// <summary>
        /// Gets or sets the delivery state.
        /// </summary>
        DeliveryState State { get; set; }

        /// <summary>
        /// Gets or sets the batchable field.
        /// </summary>
        bool Batchable { get; set; }

        /// <summary>
        /// Gets the user state object if set in the send method.
        /// </summary>
        object UserToken { get; }
        
        /// <summary>
        /// Gets or sets the Settled field.
        /// </summary>
        bool Settled { get; set; }
    }
}
