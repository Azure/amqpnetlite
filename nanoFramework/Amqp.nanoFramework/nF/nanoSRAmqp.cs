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

using static Amqp.nF.nanoSRAmqp;

namespace Amqp
{
    public partial class SRAmqp
    {
        public static string AmqpHandleExceeded
        {
            get { return GetString(StringResources.AmqpHandleExceeded); }
        }

        public static string AmqpProtocolMismatch
        {
            get { return GetString(StringResources.AmqpProtocolMismatch); }
        }

        public static string InvalidMapKeyType
        {
            get { return GetString(StringResources.InvalidMapKeyType); }
        }

        public static string LinkNotFound
        {
            get { return GetString(StringResources.LinkNotFound); }
        }

        public static string AmqpUnknownDescriptor
        {
            get { return GetString(StringResources.AmqpUnknownDescriptor); }
        }

        public static string SaslNegoFailed
        {
            get { return GetString(StringResources.SaslNegoFailed); }
        }

        public static string AmqpTimeout
        {
            get { return GetString(StringResources.AmqpTimeout); }
        }

        public static string InvalidAddressFormat
        {
            get { return GetString(StringResources.InvalidAddressFormat); }
        }

        public static string InvalidDeliveryIdOnTransfer
        {
            get { return GetString(StringResources.InvalidDeliveryIdOnTransfer); }
        }

        public static string AmqpOperationNotSupported
        {
            get { return GetString(StringResources.AmqpOperationNotSupported); }
        }

        public static string AmqpHandleNotFound
        {
            get { return GetString(StringResources.AmqpHandleNotFound); }
        }

        public static string AmqpHandleInUse
        {
            get { return GetString(StringResources.AmqpHandleInUse); }
        }

        public static string InvalidFrameSize
        {
            get { return GetString(StringResources.InvalidFrameSize); }
        }

        public static string InvalidSequenceNumberComparison
        {
            get { return GetString(StringResources.InvalidSequenceNumberComparison); }
        }

        public static string AmqpInvalidFormatCode
        {
            get { return GetString(StringResources.AmqpInvalidFormatCode); }
        }

        public static string DeliveryLimitExceeded
        {
            get { return GetString(StringResources.DeliveryLimitExceeded); }
        }

        public static string EncodingTypeNotSupported
        {
            get { return GetString(StringResources.EncodingTypeNotSupported); }
        }

        public static string AmqpChannelNotFound
        {
            get { return GetString(StringResources.AmqpChannelNotFound); }
        }

        public static string AmqpIllegalOperationState
        {
            get { return GetString(StringResources.AmqpIllegalOperationState); }
        }

        public static string InvalidMapCount
        {
            get { return GetString(StringResources.InvalidMapCount); }
        }

        public static string WindowViolation
        {
            get { return GetString(StringResources.WindowViolation); }
        }

        public static string TransportClosed
        {
            get { return GetString(StringResources.TransportClosed); }
        }
    }
}