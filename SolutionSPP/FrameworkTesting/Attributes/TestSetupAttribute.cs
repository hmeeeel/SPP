using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameworkTesting.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TestSetupAttribute : Attribute { }
}
