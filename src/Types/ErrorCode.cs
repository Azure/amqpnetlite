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

namespace Amqp.Types
{
    public static class ErrorCode
    {
        // amqp errors
        public const string InternalError = "amqp:internal-error";
        public const string NotFound = "amqp:not-found";
        public const string UnauthorizedAccess = "amqp:unauthorized-access";
        public const string DecodeError = "amqp:decode-error";
        public const string ResourceLimitExceeded = "amqp:resource-limit-exceeded";
        public const string NotAllowed = "amqp:not-allowed";
        public const string InvalidField = "amqp:invalid-field";
        public const string NotImplemented = "amqp:not-implemented";
        public const string ResourceLocked = "amqp:resource-locked";
        public const string PreconditionFailed = "amqp:precondition-failed";
        public const string ResourceDeleted = "amqp:resource-deleted";
        public const string IllegalState = "amqp:illegal-state";
        public const string FrameSizeTooSmall = "amqp:frame-size-too-small";

        // connection errors
        public const string ConnectionForced = "amqp:connection:forced";
        public const string FramingError = "amqp:connection:framing-error";
        public const string ConnectionRedirect = "amqp:connection:redirect";
        
        // session errors
        public const string WindowViolation = "amqp:session:window-violation";
        public const string ErrantLink = "amqp:session-errant-link";
        public const string HandleInUse = "amqp:session:handle-in-use";
        public const string UnattachedHandle = "amqp:session:unattached-handle";
        
        // link errors
        public const string DetachForced = "amqp:link:detach-forced";
        public const string TransferLimitExceeded = "amqp:link:transfer-limit-exceeded";
        public const string MessageSizeExceeded = "amqp:link:message-size-exceeded";
        public const string LinkRedirect = "amqp:link:redirect";
        public const string Stolen = "amqp:link:stolen";

        // tx error conditions
        public const string TransactionUnknownId = "amqp:transaction:unknown-id";
        public const string TransactionRollback = "amqp:transaction:rollback";
        public const string TransactionTimeout = "amqp:transaction:timeout";

        // messaging
        public const string MessageReleased = "amqp:message:released";
    }
}
