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

using System;
using Test.Common;

namespace LongHaulTest
{
    abstract class CommonArguments : Arguments
    {
        protected CommonArguments(string[] args, int start)
            : base(args, start)
        {
        }

        [Argument(Name = "address", Shortcut = "a", Description = "address of the remote peer or the local listener", Default = "amqp://guest:guest@127.0.0.1:5672")]
        public string Address
        {
            get;
            protected set;
        }

        [Argument(Name = "host", Shortcut = "h", Description = "host name to set on open", Default = null)]
        public string Host
        {
            get;
            protected set;
        }

        [Argument(Name = "node", Shortcut = "n", Description = "name of the AMQP node", Default = "q1")]
        public string Node
        {
            get;
            protected set;
        }

        [Argument(Name = "sync", Shortcut = "s", Description = "start a role using synchronous API", Default = true)]
        public bool Synchronous
        {
            get;
            protected set;
        }

        [Argument(Name = "async", Shortcut = "y", Description = "start a role using asynchronous API", Default = true)]
        public bool Asynchronous
        {
            get;
            protected set;
        }

        [Argument(Name = "duration", Shortcut = "d", Description = "duration in seconds (0=infinite)", Default = 5 * 60)]
        public int Duration
        {
            get;
            protected set;
        }

        [Argument(Name = "progess", Shortcut = "p", Description = "duration in seconds to report progress", Default = 10)]
        public int Progress
        {
            get;
            protected set;
        }

        [Argument(Name = "trace", Shortcut = "t", Description = "trace level: err|warn|info|verbose|frm", Default = "info")]
        public string Trace
        {
            get;
            set;
        }
    }
}
