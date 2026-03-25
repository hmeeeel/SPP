using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameworkTesting.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ExpectedExceptionAttribute : Attribute
    {
        public Type ExceptionType { get; }
        public string? MessageContains { get; set; }

        public ExpectedExceptionAttribute(Type exceptionType)
        {
            if (!typeof(Exception).IsAssignableFrom(exceptionType))
                throw new ArgumentException($"Тип {exceptionType.Name} не является Exception");
            ExceptionType = exceptionType;
        }
    }
}
