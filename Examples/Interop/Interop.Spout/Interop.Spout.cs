/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Amqp;
using Amqp.Framing;
using Amqp.Types;

namespace Examples.Interop {
    class Spout {
        //
        // Sample invocation: Interop.Spout.exe --broker localhost:5672 --timeout 30 --address my-queue
        //
        static int Main(string[] args) {
            const int ERROR_SUCCESS = 0;
            const int ERROR_OTHER = 2;

            int exitCode = ERROR_SUCCESS;
            Connection connection = null;
            try
            {
                Options options = new Options(args);

                Address address = new Address(options.Url);
                connection = new Connection(address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-spout", options.Address);
                // TODO: ReplyTo

                Stopwatch stopwatch = new Stopwatch();
                TimeSpan timespan = new TimeSpan(0, 0, options.Timeout);
                stopwatch.Start();
                for (int nSent = 0;
                    (0 == options.Count || nSent < options.Count) &&
                    (0 == options.Timeout || stopwatch.Elapsed <= timespan);
                    nSent++)
                {
                    string id = options.Id;
                    if (id.Equals(""))
                    {
                        Guid g = Guid.NewGuid();
                        id = g.ToString();
                    }
                    id += ":" + nSent.ToString();

                    Message message = new Message(options.Content);
                    message.Properties = new Properties() { MessageId = id };
                    sender.Send(message);
                    if (options.Print)
                    {
                        Console.WriteLine("Message(Properties={0}, ApplicationProperties={1}, Body={2}",
                                      message.Properties, message.ApplicationProperties, message.Body);
                    }
                }
                sender.Close();
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
