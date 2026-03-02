using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameworkTesting.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class DataRowAttribute : Attribute
    {
        public object?[] Values { get; }
        public string? DisplayName { get; set; }

        public DataRowAttribute(params object?[] values)
        {
            Values = values;
        }
    }
}
