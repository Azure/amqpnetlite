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
    /// <summary>
    /// Defines the error codes.
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>
        /// No handle (session channel or link handle) can be allocated.
        /// </summary>
        ClientNoHandleAvailable = 1000,
        /// <summary>
        /// The handle (session channel or link handle) is already assigned.
        /// </summary>
        ClientHandlInUse = 1001,
        /// <summary>
        /// The operation times out.
        /// </summary>
        ClientWaitTimeout = 1002,
        /// <summary>
        /// The received SASL performatives do not have correct item count.
        /// </summary>
        ClientInitializeWrongBodyCount = 1003,
        /// <summary>
        /// The received SASL mechanisms do not contain the expected value.
        /// </summary>
        ClientInitializeWrongSymbol = 1004,
        /// <summary>
        /// The received protocol header does not match the client config.
        /// </summary>
        ClientInitializeHeaderCheckFailed = 1005,
        /// <summary>
        /// The SASL negotiation failed.
        /// </summary>
        ClientInitializeSaslFailed = 1006,
        /// <summary>
        /// An invalid link handle is received.
        /// </summary>
        ClientInvalidHandle = 1007,
        /// <summary>
        /// The requested link was not found (by handle or by name).
        /// </summary>
        ClientLinkNotFound = 1008,
        /// <summary>
        /// The received frame has an invalid performative code.
        /// </summary>
        ClientInvalidCodeOnFrame = 1009,
        /// <summary>
        /// The format code from the buffer is invalid.
        /// </summary>
        ClientInvalidFormatCodeRead = 1010,
        /// <summary>
        /// The frame type is invalid.
        /// </summary>
        ClientInvalidFrameType = 1011,
        /// <summary>
        /// The session channel is invalid.
        /// </summary>
        ClientInvalidChannel = 1012,
        /// <summary>
        /// The descriptor code is invalid.
        /// </summary>
        ClientInvalidCode = 1013,
        /// <summary>
        /// The list in the performative is invalid.
        /// </summary>
        ClientInvalidFieldList = 1014,
        /// <summary>
        /// The payload for a performative is invalid.
        /// </summary>
        ClientInvalidPayload = 1015,
        /// <summary>
        /// The property is not allowed to set after the client is connected.
        /// </summary>
        ClientNotAllowedAfterConnect = 1016,
        /// <summary>
        /// The client is not connected.
        /// </summary>
        ClientNotConnected = 1017,

        /// <summary>
        /// The receiver is not in a invalid state to start.
        /// </summary>
        ReceiverStartInvalidState = 2000,
        /// <summary>
        /// The receiver is not in a invalid state to accept a message.
        /// </summary>
        ReceiverAcceptInvalidState = 2001,
        /// <summary>
        /// The receiver does not have link credit for an incoming message.
        /// </summary>
        InvalidCreditOnTransfer = 2002,

        /// <summary>
        /// The sender is not in a valid state to send a message.
        /// </summary>
        SenderSendInvalidState = 3000
    }
}
