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
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Amqp.Types;

    /// <summary>
    /// A <seealso cref="SaslProfile"/> that processes commands asynchronously.
    /// </summary>
    public abstract class AsyncSaslProfile : SaslProfile
    {
        /// <summary>
        /// <inheritdoc cref="SaslProfile.SaslProfile(Symbol)"/>
        /// </summary>
        public AsyncSaslProfile(Symbol mechanism)
            : base(mechanism)
        {
        }

        /// <summary>
        /// Processes the received command asynchronously and returns a response. If returns
        /// null, the SASL handshake completes.
        /// </summary>
        /// <param name="command">The SASL command received from the peer.</param>
        /// <returns>A Task that returns a SASL command as a response to the incoming command.</returns>
        protected abstract Task<DescribedList> OnCommandAsync(DescribedList command);

        /// <summary>
        /// <inheritdoc cref="SaslProfile.OnCommand(DescribedList)"/>
        /// </summary>
        protected sealed override DescribedList OnCommand(DescribedList command)
        {
            var task = this.OnCommandAsync(command);
            return new ResponseTask(task);
        }

        // Just a wrapper of task that also satisfies the OnCommand contract.
        internal sealed class ResponseTask : DescribedList
        {
            static readonly Descriptor descriptor = new Descriptor(ulong.MaxValue, string.Empty);
            readonly Task<DescribedList> task;

            public ResponseTask(Task<DescribedList> task)
                : base(descriptor, 0)
            {
                this.task = task;
            }

            public Task<DescribedList> Task => this.task;

            internal override void ReadField(ByteBuffer buffer, int index, byte formatCode)
            {
                throw new NotImplementedException();
            }

            internal override void WriteField(ByteBuffer buffer, int index)
            {
                throw new NotImplementedException();
            }
        }
    }
}