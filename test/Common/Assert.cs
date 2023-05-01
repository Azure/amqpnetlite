using Amqp.Framing;
using System;
using System.Threading;
#if NETMF && !NANOFRAMEWORK_1_0
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
            bool areEqual = false;
            if (expected == null || actual == null)
            {
                areEqual = expected == actual;
            }
            else if (expected is Type && actual is Type)
            {
                var t1 = (Type)expected;
                var t2 = (Type)actual;
                areEqual = t1 == t2 || (t1 == typeof(DataList) && t2 == typeof(Data));
            }
            else if (expected is byte[] && actual is byte[])
            {
                byte[] a = (byte[])expected;
                byte[] b = (byte[])actual;
                if (a.Length == b.Length)
                {
                    areEqual = true;
                    for (int i = 0; i < a.Length; i++)
                    {
                        if (a[i] != b[i])
                        {
                            areEqual = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                areEqual = expected.Equals(actual);
            }

            if (!areEqual)
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
