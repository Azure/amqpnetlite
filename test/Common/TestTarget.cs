//  ------------------------------------------------------------------------------------
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

using System;
using Amqp;

namespace Test.Amqp
{
    /// <summary>
    /// For self tests that create AMQP connections to a broker
    /// define the URI of the broker and the name of the queue to use.
    ///
    /// The default broker on localhost may be overridden with
    /// environment variable AMQPNETLITE_TESTTARGET. A single URI
    /// specifies both the broker (scheme, user, password, host, port)
    /// and source or target (path) of the broker resource to be
    /// exercised by the self tests.
    /// </summary>
    public class TestTarget
    {
        internal const string envVarName = "AMQPNETLITE_TESTTARGET";
        internal const string defaultAddress = "amqp://guest:guest@localhost:5672/q1";
        internal string address;
        internal string path;

        public TestTarget()
        {
#if !COMPACT_FRAMEWORK && !NETFX_CORE && !NETMF
            this.address = Environment.GetEnvironmentVariable(envVarName);
#endif
            if (this.address == null)
            {
                this.address = defaultAddress;
            }

            // Verify that the URI is well formed.
            Address addr = new Address(this.address);
            // Extract the path without the leading "/".
            path = addr.Path.Substring(1);
        }

        public string Path
        {
            get
            {
                return path;
            }
        }

        public Address Address
        {
            get
            {
                return new Address(address);
            }
        }
    }
}
