using FrameworkTesting.Attributes;
using System.Reflection;

namespace FrameworkTesting.Filtering
{
    public delegate bool TestFilterDelegate(TestFilterContext context);

    public class TestFilterContext
    {
        public Type TestClass { get; set; } = null!;
        public MethodInfo? TestMethod { get; set; }
        public string ClassName => TestClass.Name;
        public string MethodName => TestMethod?.Name ?? string.Empty;
        public TestClassAttribute? ClassAttribute { get; set; }
        public TestMethodAttribute? MethodAttribute { get; set; }
    }

}