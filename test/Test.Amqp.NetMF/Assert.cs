using Microsoft.SPOT;

namespace Test.Amqp
{
    static class Assert
    {
        public static void IsTrue(bool condition, string message = null)
        {
            Debug.Assert(condition, message ?? "Condition is not true.");
        }

        public static void AreEqual(object expected, object actual, string message = null)
        {
            Debug.Assert((expected == null && actual == null) || expected.Equals(actual),
                message ?? "Not equal. Expected: " + expected + ", Actual: " + (actual ?? "<NULL>"));
        }
    }
}
