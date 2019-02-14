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
    using System;

    enum RestoreCondition
    {
        OnReceive,
        OnAck,
        None
    };

    /// <summary>
    /// Defines the mode to restore link credits in a <see cref="ReceiverLink"/>
    /// and a threshold of restored credits to send a flow.
    /// </summary>
    public sealed class CreditMode
    {
        /// <summary>
        /// Defines a mode that link credits are restored after a message is received. Threshold for flow is default.
        /// </summary>
        public static readonly CreditMode RestoreOnReceive =
            new CreditMode() { Condition = RestoreCondition.OnReceive, Threshold = -1 };

        /// <summary>
        /// Defines a mode that link credits are restored after a message is acknowledged. Threshold for flow is default.
        /// </summary>
        public static readonly CreditMode RestoreOnAcknowledge =
            new CreditMode() { Condition = RestoreCondition.OnAck, Threshold = -1 };

        /// <summary>
        /// Defines a mode that link credits are drained.
        /// </summary>
        public static readonly CreditMode Drain =
            new CreditMode() { IsDrain = true };

        /// <summary>
        /// Defines a mode that link credits are managed entirely by the application.
        /// </summary>
        public static readonly CreditMode Manual =
            new CreditMode() { Condition = RestoreCondition.None };

        internal RestoreCondition Condition
        {
            get;
            private set;
        }

        internal int Threshold
        {
            get;
            private set;
        }

        internal bool IsDrain
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates a CreditMode that auto-restores link credit on receive calls with a custom flow threshold.
        /// </summary>
        /// <param name="threshold">The threshold of restored credits to send a flow.</param>
        /// <returns>A CreditMode object.</returns>
        public static CreditMode OnReceive(int threshold)
        {
            return new CreditMode() { Condition = RestoreCondition.OnReceive, Threshold = threshold };
        }

        /// <summary>
        /// Creates a CreditMode that auto-restores link credit on Accept/Reject/Release/Modify calls
        /// with a custom flow threshold.
        /// </summary>
        /// <param name="threshold">The threshold of restored credits to send a flow.</param>
        /// <returns>A CreditMode object.</returns>
        public static CreditMode OnAcknowledge(int threshold)
        {
            return new CreditMode() { Condition = RestoreCondition.OnReceive, Threshold = threshold };
        }
    }
}