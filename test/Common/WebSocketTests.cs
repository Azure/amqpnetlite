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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Sasl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trace = Amqp.Trace;
using TraceLevel = Amqp.TraceLevel;

namespace Test.Amqp
{
    [TestClass]
    public class WebSocketTests
    {
        const string address = "ws://guest:guest@localhost:18080/test";

#if NETFX
        [TestMethod]
        public void WebSocketContainerHostTests()
        {
            int total = 0;
            int passed = 0;
            string[] excluded = new string[]
            {
                "ContainerHostCustomTransportTest"
            };

            foreach (var mi in typeof(ContainerHostTests).GetMethods())
            {
                if (mi.GetCustomAttributes(typeof(TestMethodAttribute), false).Length > 0 &&
                    mi.GetCustomAttributes(typeof(IgnoreAttribute), false).Length == 0 &&
                    !excluded.Contains(mi.Name))
                {
                    total++;

                    ContainerHostTests test = new ContainerHostTests();
                    test.Address = new Address(address);
                    test.ClassInitialize();

                    try
                    {
                        mi.Invoke(test, new object[0]);
                        System.Diagnostics.Trace.WriteLine(mi.Name + " passed");
                        passed++;
                    }
                    catch (Exception exception)
                    {
                        System.Diagnostics.Trace.WriteLine(mi.Name + " failed: " + exception.ToString());
                    }

                    test.ClassCleanup();
                }
            }

            Assert.AreEqual(total, passed, string.Format("Not all tests passed {0}/{1}", passed, total));
        }

        [TestMethod]
        public async Task WebSocketSslMutalAuthTest()
        {
            string testName = "WebSocketSslMutalAuthTest";
            Address listenAddress = new Address("wss://localhost:18081/" + testName + "/");

            string output;
            int code = Exec("netsh.exe", string.Format("http show sslcert hostnameport={0}:{1}", listenAddress.Host, listenAddress.Port), out output);
            if (code != 0)
            {
                X509Certificate2 cert = null;
                Test.Common.Extensions.TryGetCertificate(StoreLocation.LocalMachine, StoreName.My, "localhost", out cert);
                Assert.IsTrue(cert != null, "Failed to get dev-cert from LocalMachine store");
                string args = string.Format("http add sslcert hostnameport={0}:{1} certhash={2} certstorename=MY appid={{{3}}} clientcertnegotiation=enable",
                    listenAddress.Host, listenAddress.Port, cert.Thumbprint, Guid.NewGuid());
                Debug.WriteLine(args);
                code = Exec("netsh.exe", args, out output);
                Assert.AreEqual(0, code, "failed to add ssl cert: " + output);
            }

            X509Certificate serviceCert = null;
            X509Certificate clientCert = null;
            ListenerLink listenerLink = null;

            var linkProcessor = new TestLinkProcessor();
            linkProcessor.SetHandler(a => { listenerLink = a.Link; return false; });
            var host = new ContainerHost(listenAddress);
            host.Listeners[0].SASL.EnableExternalMechanism = true;
            host.Listeners[0].SSL.ClientCertificateRequired = true;
            host.Listeners[0].SSL.CheckCertificateRevocation = true;
            host.Listeners[0].SSL.RemoteCertificateValidationCallback = (a, b, c, d) => { clientCert = b; return true; };
            host.RegisterLinkProcessor(linkProcessor);
            host.Open();

            try
            {
                ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => { serviceCert = b; return true; };
                var wssFactory = new WebSocketTransportFactory();
                wssFactory.Options = o =>
                {
                    o.ClientCertificates.Add(Test.Common.Extensions.GetCertificate(listenAddress.Host));
                };

                ConnectionFactory connectionFactory = new ConnectionFactory(new TransportProvider[] { wssFactory });
                connectionFactory.SASL.Profile = SaslProfile.External;
                Connection connection = await connectionFactory.CreateAsync(listenAddress);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "q1");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });                
                await connection.CloseAsync();

                Assert.IsTrue(serviceCert != null, "service cert not received");
                Assert.IsTrue(clientCert != null, "client cert not received");
                Assert.IsTrue(listenerLink != null, "link not attached");

                IPrincipal principal = ((ListenerConnection)listenerLink.Session.Connection).Principal;
                Assert.IsTrue(principal != null, "connection pricipal is null");
                Assert.IsTrue(principal.Identity is X509Identity, "identify should be established by client cert");
            }
            finally
            {
                host.Close();
            }
        }

        static int Exec(string cmd, string args, out string output)
        {
            ProcessStartInfo psi = new ProcessStartInfo(cmd, args);
            psi.RedirectStandardOutput = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = false;
            Process process = Process.Start(psi);
            process.WaitForExit();
            output = process.StandardOutput.ReadToEnd();
            return process.ExitCode;
        }
#endif

        [TestMethod]
        public async Task WebSocketSendReceiveAsync()
        {
            if (Environment.GetEnvironmentVariable("CoreBroker") == "1")
            {
                // No Websocket listener on .Net Core
                return;
            }

            string testName = "WebSocketSendReceiveAsync";

            // assuming it matches the broker's setup and port is not taken
            Address wsAddress = new Address(address);
            int nMsgs = 50;

            ConnectionFactory connectionFactory = new ConnectionFactory(
                new TransportProvider[] { new WebSocketTransportFactory() });
            Connection connection = await connectionFactory.CreateAsync(wsAddress);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, "q1");

            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message();
                message.Properties = new Properties() { MessageId = "msg" + i, GroupId = testName };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                await sender.SendAsync(message);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "q1");
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = await receiver.ReceiveAsync();
                Trace.WriteLine(TraceLevel.Information, "receive: {0}", message.ApplicationProperties["sn"]);
                receiver.Accept(message);
            }

            await sender.CloseAsync();
            await receiver.CloseAsync();
            await session.CloseAsync();
            await connection.CloseAsync();
        }
    }
}
