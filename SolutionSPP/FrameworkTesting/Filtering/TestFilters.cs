using System.Reflection;
using FrameworkTesting.Attributes;
using FrameworkTesting.Filtering;

public static class TestFilters
    {
        public static TestFilterDelegate ByCategory(string category)
        {
            return ctx => ctx.ClassAttribute?.Category == category;
        }

        public static TestFilterDelegate ByCategories(params string[] categories)
        {
            return ctx => categories.Contains(ctx.ClassAttribute?.Category);
        }

        public static TestFilterDelegate ByClassName(string namePattern)
        {
            return ctx => ctx.ClassName.Contains(namePattern, StringComparison.OrdinalIgnoreCase);
        }

        public static TestFilterDelegate ByMethodName(string namePattern)
        {
            return ctx => ctx.MethodName.Contains(namePattern, StringComparison.OrdinalIgnoreCase);
        }
        public static TestFilterDelegate NotIgnored()
        {
            return ctx =>
            {
                bool classIgnored = ctx.TestClass.GetCustomAttribute<IgnoreAttribute>() != null;
                bool methodIgnored = ctx.TestMethod?.GetCustomAttribute<IgnoreAttribute>() != null;
                return !classIgnored && !methodIgnored;
            };
        }
        public static TestFilterDelegate Parameterized()
        {
            return ctx => ctx.TestMethod?.GetCustomAttributes<DataRowAttribute>().Any() ?? false;
        }
        public static TestFilterDelegate WithTimeout()
        {
            return ctx => ctx.TestMethod?.GetCustomAttribute<TimeoutAttribute>() != null;
        }


        public static TestFilterDelegate ExpectsException()
        {
            return ctx => ctx.TestMethod?.GetCustomAttribute<ExpectedExceptionAttribute>() != null;
        }

        public static TestFilterDelegate And(TestFilterDelegate filter1, TestFilterDelegate filter2)
        {
            return ctx => filter1(ctx) && filter2(ctx);
        }

        public static TestFilterDelegate Or(TestFilterDelegate filter1, TestFilterDelegate filter2)
        {
            return ctx => filter1(ctx) || filter2(ctx);
        }

        public static TestFilterDelegate Not(TestFilterDelegate filter)
        {
            return ctx => !filter(ctx);
        }

        public static TestFilterDelegate AndAll(params TestFilterDelegate[] filters)
        {
            return ctx => filters.All(f => f(ctx));
        }

        public static TestFilterDelegate OrAny(params TestFilterDelegate[] filters)
        {
            return ctx => filters.Any(f => f(ctx));
        }


        public static TestFilterDelegate Custom(Func<TestFilterContext, bool> predicate)
        {
            return new TestFilterDelegate(predicate);
        }


        public static IEnumerable<Type> ApplyClassFilter(
            this IEnumerable<Type> classes,
            TestFilterDelegate filter)
        {
            foreach (var cls in classes)
            {
                var context = new TestFilterContext
                {
                    TestClass = cls,
                    ClassAttribute = cls.GetCustomAttribute<TestClassAttribute>()
                };

                if (filter(context))
                    yield return cls;
            }
        }

        public static IEnumerable<MethodInfo> ApplyFilter(
            this IEnumerable<MethodInfo> methods,
            Type testClass,
            TestFilterDelegate filter)
        {
            var classAttr = testClass.GetCustomAttribute<TestClassAttribute>();

            foreach (var method in methods)
            {
                var context = new TestFilterContext
                {
                    TestClass = testClass,
                    TestMethod = method,
                    ClassAttribute = classAttr,
                    MethodAttribute = method.GetCustomAttribute<TestMethodAttribute>()
                };

                if (filter(context))
                    yield return method;
            }
        }

    }