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

//
// ReconnectSender 
//  * Detects a failed AMQP connection and automatically reconnects it.
//  * Recovers from a peer failure by connecting to a list of AMQP brokers/peers.
//  * Builds or rebuilds the AMQP object hierarchy in response to protocol events.
//  * Recovers from all failures by reconnecting the AMQP connection.
//
// Command line:
//   ReconnectSender [brokerUrl-csv-string [message-count]]
//
// Default:
//   ReconnectSender amqp://127.0.0.1:5672/q1,amqp://127.0.0.1:15672/q1 10
//
// Requires:
//   A broker or peer at the addresses given in arg 1.
//
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Amqp;
using Amqp.Framing;

namespace ReconnectSender
{
    public class Application
    {
        // AMQP connection selection
        List<Address> addresses;
        int aIndex = 0;

        // Protocol objects
        Connection connection;
        Session session;
        SenderLink sender;

        // Sender is ready to send messages 
        ManualResetEvent sendable = new ManualResetEvent(false);

        // Time in mS to wait for a connection to connect and become sendable
        // before failing over to the next host.
        const Int32 SENDABLE_WAIT_TIME = 10 * 1000;

        // Application mission state
        ulong nToSend = 0;
        ulong nSent = 0;

        /// <summary>
        /// Application constructor
        /// </summary>
        /// <param name="_addresses">Address objects that define the host, port, and target for messages.</param>
        /// <param name="_nToSend">Message count.</param>
        public Application(List<Address> _addresses, ulong _nToSend)
        {
            addresses = _addresses;
            nToSend = _nToSend;
        }

        /// <summary>
        /// Connection closed event handler
        /// 
        /// This function provides information only. Calling Reconnect is redundant with
        /// calls from the Run loop.
        /// </summary>
        /// <param name="_">Connection that closed. There is only one connection so this is ignored.</param>
        /// <param name="error">Error object associated with connection close.</param>
        void connectionClosed(IAmqpObject _, Error error)
        {
            if (error == null)
                Trace.WriteLine(TraceLevel.Warning, "Connection closed with no error");
            else
                Trace.WriteLine(TraceLevel.Error, "Connection closed with error: {0}", error.ToString());
        }

        /// <summary>
        /// Select the next host in the Address list and start it
        /// </summary>
        void Reconnect()
        {
            Trace.WriteLine(TraceLevel.Verbose, "Entering Reconnect()");

            sendable.Reset();

            if (nSent < nToSend)
            {
                if (++aIndex >= addresses.Count) aIndex = 0;
                OpenConnection();
            }
        }


        /// <summary>
        /// Start the current host in the address list
        /// </summary>
        async void OpenConnection()
        {
            try
            {
                Trace.WriteLine(TraceLevel.Verbose, 
                    "Attempting connection to  {0}:{1}",
                    addresses[aIndex].Host, addresses[aIndex].Port);

                connection = await Connection.Factory.CreateAsync(addresses[aIndex], null, onOpened);

                Trace.WriteLine(TraceLevel.Information,
                    "Success: connecting to {0}:{1}",
                    addresses[aIndex].Host, addresses[aIndex].Port);

                connection.AddClosedCallback(connectionClosed);
            }
            catch (Exception e)
            {
                Trace.WriteLine(TraceLevel.Error,
                    "Failure: exception connecting to '{0}:{1}': {2}",
                    addresses[aIndex].Host, addresses[aIndex].Port, e.Message);
            }
        }

        /// <summary>
        /// AMQP connection has opened. This callback may be called before
        /// ConnectAsync returns a value to the _connection_ variable.
        /// </summary>
        /// <param name="conn">Which connection. </param>
        /// <param name="__">Peer AMQP Open (ignored).</param>
        void onOpened(IConnection conn, Open __)
        {
            Trace.WriteLine(TraceLevel.Verbose, "Event: OnOpened");

            connection = (Connection)conn;

            session = new Session(connection, new Begin() { }, onBegin);
        }

        /// <summary>
        /// AMQP session has opened
        /// </summary>
        /// <param name="_">Which session (ignored).</param>
        /// <param name="__">Peer AMQP Begin (ignored).</param>
        void onBegin(ISession _, Begin __)
        {
            Trace.WriteLine(TraceLevel.Verbose, "Event: OnBegin");

            string targetName = addresses[aIndex].Path.Substring(1); // no leading '/'
            Target target = new Target() { Address = targetName };
            sender = new SenderLink(session, "senderLink", target, onAttached);
        }

        /// <summary>
        /// AMQP Link has attached. Signal that protocol stack is ready to send.
        /// </summary>
        /// <param name="_">Which link (ignored).</param>
        /// <param name="__">Peer AMQP Attach (ignored).</param>
        void onAttached(ILink _, Attach __)
        {
            Trace.WriteLine(TraceLevel.Verbose, "Event: OnAttached");

            sendable.Set();
        }

        /// <summary>
        /// Application mission code.
        /// Send N messages while automatically reconnecting to broker/peer as necessary.
        /// </summary>
        void Run()
        {
            OpenConnection();
            while (nSent < nToSend)
            {
                if (sendable.WaitOne(SENDABLE_WAIT_TIME))
                {
                    try
                    {
                        Trace.WriteLine(TraceLevel.Information, "Sending message {0}", nSent);

                        Message message = new Message("message " + nSent.ToString());
                        message.Properties = new Properties();
                        message.Properties.SetMessageId((object)nSent);
                        sender.Send(message);
                        nSent += 1;

                        Trace.WriteLine(TraceLevel.Information, "Sent    message {0}", nSent-1);
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(TraceLevel.Error, 
                            "Exception sending message {0}: {1}", nSent, e.Message);
                        Reconnect();
                    }
                }
                else
                {
                    Trace.WriteLine(TraceLevel.Warning, "Timeout waiting for connection");
                    Reconnect();
                }
            }
        }

        public static void Main(string[] args)
        {
            string addrs = args.Length >= 1 ? args[0] : "amqp://127.0.0.1:5672/q1,amqp://127.0.0.1:15672/q1";
            ulong count = args.Length >= 2 ? Convert.ToUInt64(args[1]) : 10;

            Trace.TraceLevel = TraceLevel.Verbose;
            Trace.TraceListener = (l, f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));

            List<Address> addresses = new List<Address>();
            foreach (var adr in addrs.Split(',').ToList()) addresses.Add(new Address(adr));

            Application app = new Application(addresses, count);
            app.Run();
        }
    }
}
