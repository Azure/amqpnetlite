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

namespace Amqp.Sasl
{
    /// <summary>
    /// Codes to indicate the outcome of the SASL dialog.
    /// </summary>
    public enum SaslCode : byte
    {
        /// <summary>
        /// Connection authentication succeeded.
        /// </summary>
        Ok = 0,

        /// <summary>
        /// Connection authentication failed due to an unspecified problem with the supplied credentials.
        /// </summary>
        Auth = 1,

        /// <summary>
        /// Connection authentication failed due to a system error.
        /// </summary>
        Sys = 2,

        /// <summary>
        /// Connection authentication failed due to a system error that is unlikely to be corrected without intervention.
        /// </summary>
        SysPerm = 3,

        /// <summary>
        /// Connection authentication failed due to a transient system error.
        /// </summary>
        SysTemp = 4
    }
}