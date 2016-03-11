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

namespace Amqp.Listener
{
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Principal;

    /// <summary>
    /// Represents an client identity established by SSL client certificate authentication.
    /// </summary>
    public class X509Identity : IIdentity
    {
        internal X509Identity(X509Certificate certificate)
        {
            this.Certificate = certificate;
        }

        /// <summary>
        /// Gets the client certificate.
        /// </summary>
        public X509Certificate Certificate
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the type of authentication used. ("X509").
        /// </summary>
        public string AuthenticationType
        {
            get { return "X509"; }
        }

        /// <summary>
        /// Gets a value that indicates whether the user has been authenticated.
        /// </summary>
        public bool IsAuthenticated
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the name of the identity.
        /// </summary>
        public string Name
        {
            get { return this.Certificate.Subject; }
        }
    }
}
