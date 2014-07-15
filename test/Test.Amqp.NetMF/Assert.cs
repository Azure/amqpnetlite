using System;
#if NETMF
using Microsoft.SPOT;
#endif

namespace Test.Amqp
{
    static class Assert
    {
        public static void IsTrue(bool condition, string message = null)
        {
            if(!condition)
            {
                throw new Exception(message ?? "Condition is not true.");
            }
        }

        public static void AreEqual(object expected, object actual, string message = null)
        {
            if (!((expected == null && actual == null) || expected.Equals(actual)))
            {
                throw new Exception(message ?? "Not equal. Expected: " + expected + ", Actual: " + (actual ?? "<NULL>"));
            }
        }
    }
}
