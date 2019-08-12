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
using Amqp.Handler;

namespace Test.Amqp
{
    public class TestHandler : IHandler
    {
        readonly Action<Event> action;

        public TestHandler(Action<Event> action)
        {
            this.action = action;
        }

        bool IHandler.CanHandle(EventId id)
        {
            return true;
        }

        void IHandler.Handle(Event protocolEvent)
        {
            this.action(protocolEvent);
        }
    }
}
