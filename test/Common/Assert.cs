using System;
using System.Threading;
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

#if COMPACT_FRAMEWORK
        public static bool WaitOne(this ManualResetEvent e, int ms)
        {
            return e.WaitOne(ms, false);
        }
#endif
    }

#if DOTNET
    static class CollectionAssert
    {
        public static void AreEqual(System.Collections.ICollection expected, System.Collections.ICollection actual)
        {
            if (expected == null && actual == null)
            {
                return;
            }

            if (expected == null || actual == null)
            {
                Assert.IsTrue(false, expected == null ? "expected" : "actual" + " is null");
            }

            Assert.AreEqual(expected.Count, actual.Count, "Count not equal");

            var ie = expected.GetEnumerator();
            var ia = actual.GetEnumerator();
            while (true)
            {
                bool b1 = ie.MoveNext();
                bool b2 = ia.MoveNext();
                Assert.AreEqual(b1, b2);

                if (!b1) break;
                Assert.AreEqual(ie.Current, ia.Current);
            }
        }
    }
#endif
}
