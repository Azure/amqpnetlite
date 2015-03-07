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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amqp;
using Amqp.Framing;
using Amqp.Types;

namespace Examples.Interop {
    class Drain {
        //
        // Sample invocation: Interop.Drain.exe --broker localhost:5672 --timeout 30 --address my-queue
        //
        static int Main(string[] args) {
            const int ERROR_SUCCESS = 0;
            const int ERROR_NO_MESSAGE = 1;
            const int ERROR_OTHER = 2;

            int exitCode = ERROR_SUCCESS;
            Connection connection = null;
            try
            {
                Options options = new Options(args);

                Address address = new Address(options.Url);
                connection = new Connection(address);
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver-drain", options.Address);
                int timeout = int.MaxValue;
                if (!options.Forever)
                    timeout = 1000 * options.Timeout;
                Message message = new Message();
                int nReceived = 0;
                receiver.SetCredit(options.InitialCredit);
                while ((message = receiver.Receive(null, timeout)) != null)
                {
                    nReceived++;
                    if (!options.Quiet)
                    {
                        Console.WriteLine("Message(Properties={0}, ApplicationProperties={1}, Body={2}",
                                      message.Properties, message.ApplicationProperties, message.Body);
                    }
                    receiver.Accept(message);
                    if (options.Count > 0 && nReceived == options.Count)
                    {
                        break;
                    }
                }
                if (message == null)
                {
                    exitCode = ERROR_NO_MESSAGE;
                }
                receiver.Close();
                session.Close();
                connection.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception {0}.", e);
                if (null != connection)
                    connection.Close();
                exitCode = ERROR_OTHER;
            }
            return exitCode;
        }
    }
}
