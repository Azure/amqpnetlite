using System;
using Amqp;
using Listener.IContainer;
using AmqpTrace = Amqp.Trace;

namespace Test.Amqp
{
    public class Program
    {
        public static int Main(string[] args)
        {
            AmqpTrace.TraceLevel = TraceLevel.Output;
            AmqpTrace.TraceListener = Program.WriteTrace;
            Connection.DisableServerCertValidation = true;

            TestAmqpBroker broker = null;
            if (args.Length == 0)
            {
                broker = new TestAmqpBroker(new string[] { LinkTests.AddressString },
                    string.Join(":", LinkTests.address.User, LinkTests.address.Password), null, null);
                broker.Start();
            }

            try
            {
                return TestRunner.RunTests();
            }
            finally
            {
                if (broker != null)
                {
                    broker.Stop();
                }
            }
        }

        static void WriteTrace(string format, params object[] args)
        {
            string message = args == null ? format : string.Format(format, args);
            Console.WriteLine(message);
        }
    }
}
