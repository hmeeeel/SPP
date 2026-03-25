using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameworkTesting.Testing
{
    public class TestFrameworkException : Exception
    {
        public TestFrameworkException(string message, Exception? inner = null)
            : base(message, inner) { }
    }

}
