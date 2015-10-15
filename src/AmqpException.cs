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
    using Amqp.Framing;

#if !TRACE
    using Amqp.Types;
#endif

    /// <summary>
    /// The exception that is thrown when an AMQP error occurs.
    /// </summary>
    public sealed class AmqpException : Exception
    {
#if !TRACE
        /// <summary>
        /// Initializes a new instance of the AmqpException class with the AMQP error.
        /// </summary>
        /// <param name="error">The AMQP error.</param>
        public AmqpException(Error error)
        {
            this.Error = error;
        }

        /// <summary>
        /// Initializes a new instance of the AmqpException class with the AMQP error.
        /// </summary>
        /// <param name="error">The AMQP error.</param>
        /// <param name="description">The error description.</param>
        public AmqpException(Error error, string description)
        {
            this.Error = error;
        }

        /// <summary>
        /// Gets the AMQP error stored in this exception.
        /// </summary>
        public Error Error
        {
            get;
            private set;
        }
        /// <summary>
        /// Initializes a new instance of the AmqpException class with the AMQP error code.
        /// </summary>
        /// <param name="error">The AMQP error code.</param>
        public AmqpException(ErrorCode error)
        {
            this.ErrorCode = error;
        }

        /// <summary>
        /// Initializes a new instance of the AmqpException class with the AMQP error code and a description.
        /// </summary>
        /// <param name="error">The AMQP error code.</param>
        /// <param name="description">The error description.</param>
        public AmqpException(ErrorCode error, string description)
        {
            this.ErrorCode = error;
        }
        
        /// <summary>
        /// Gets the AMQP error codestored in this exception.
        /// </summary>
        public ErrorCode ErrorCode
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the description for the error stored in this exception.
        /// </summary>
        public string Description
        {
            get;
            private set;
        }
#else
        /// <summary>
        /// Initializes a new instance of the AmqpException class with the AMQP error.
        /// </summary>
        /// <param name="error">The AMQP error.</param>
        public AmqpException(Error error)
            : base(error.Description ?? error.Condition)
        {
            this.Error = error;
        }

        /// <summary>
        /// Initializes a new instance of the AmqpException class with the AMQP error.
        /// </summary>
        /// <param name="condition">The error condition.</param>
        /// <param name="description">The error description.</param>
        public AmqpException(string condition, string description)
            : this(new Error() { Condition = condition, Description = description })
        {
        }

        /// <summary>
        /// Gets the AMQP error stored in this exception.
        /// </summary>
        public Error Error
        {
            get;
            private set;
        }
#endif
    }
}
