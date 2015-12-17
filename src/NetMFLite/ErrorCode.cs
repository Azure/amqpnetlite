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
    public enum ErrorCode
    {
        // client error codes
        ClientNoHandleAvailable = 1000,
        ClientHandlInUse = 1001,
        ClientWaitTimeout = 1002,
        ClientInitializeWrongBodyCount = 1003,
        ClientInitializeWrongSymbol = 1004,
        ClientInitializeHeaderCheckFailed = 1005,
        ClientInitializeSaslFailed = 1006,
        ClientInvalidHandle = 1007,
        ClientLinkNotFound = 1008,
        ClientInvalidCodeOnFrame = 1009,
        ClientInvalidFormatCodeRead = 1010,
        ClientInvalidFrameType = 1011,
        ClientInvalidChannel = 1012,
        ClientInvalidCode = 1013,
        ClientInvalidFieldList = 1014,
        ClientInvalidPayload = 1015,
        ClientIdleTimeoutTooSmall = 1016,

        // received error codes
        ReceiverStartInvalidState = 2000,
        ReceiverAcceptInvalidState = 2001,
        InvalidCreditOnTransfer = 2002,

        // sender error codes
        SenderSendInvalidState = 3000
    }
}
