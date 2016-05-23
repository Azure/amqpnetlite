using System;
using Amqp;
using AmqpTrace = Amqp.Trace;

namespace Test.Amqp
{
    public class Program
    {
        public static int Main(string[] args)
        {
            AmqpTrace.TraceLevel = TraceLevel.Information;
            AmqpTrace.TraceListener = Program.WriteTrace;
            Connection.DisableServerCertValidation = true;

            return TestRunner.RunTests();
        }

        static void WriteTrace(string format, params object[] args)
        {
            string message = args == null ? format : string.Format(format, args);
            Console.WriteLine(message);
        }
    }
}
