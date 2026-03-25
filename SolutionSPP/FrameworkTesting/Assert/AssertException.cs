using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameworkTesting.Assert
{
    public class AssertException : Exception
    {
        public string AssertionType { get; }
        public object? Expected { get; }
        public object? Actual { get; }

        public AssertException(string message, string assertionType = "Assert",
            object? expected = null, object? actual = null)
            : base(message)
        {
            AssertionType = assertionType;
            Expected = expected;
            Actual = actual;
        }
    }
}
