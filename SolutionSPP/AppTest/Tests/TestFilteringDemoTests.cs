using FrameworkTesting.Assert;
using FrameworkTesting.Attributes;
using FrameworkTesting.Filtering;
using System.Reflection;

namespace AppTest.Tests
{
    [TestClass(Category = "Filtering", Description = "Демонстрация фильтрации через делегаты")]
    public class TestFilteringDemoTests
    {
        [TestMethod("Фильтр по категории")]
        public void FilterDemo_ByCategory()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allTestClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();

            Console.WriteLine($"Всего тестовых классов: {allTestClasses.Count}");
            TestFilterDelegate filter = TestFilters.ByCategory("Performance");

            var filteredClasses = allTestClasses.ApplyClassFilter(filter).ToList();

            Console.WriteLine($"Классов с категорией 'Performance': {filteredClasses.Count}");

            foreach (var cls in filteredClasses)
            {
                var attr = cls.GetCustomAttribute<TestClassAttribute>();
                Assert.AreEqual("Performance", attr?.Category, 
                    $"Класс {cls.Name} должен иметь категорию Performance");
            }
        }

        [TestMethod("Фильтр по имени класса")]
        public void FilterDemo_ByClassName()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allTestClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();


            TestFilterDelegate filter = TestFilters.ByClassName("Async");

            var filteredClasses = allTestClasses.ApplyClassFilter(filter).ToList();

            Console.WriteLine($"Классов с 'Async' в имени: {filteredClasses.Count}");

            foreach (var cls in filteredClasses)
            {
                Console.WriteLine($"  - {cls.Name}");
                Assert.IsTrue(cls.Name.Contains("Async", StringComparison.OrdinalIgnoreCase),
                    $"Класс {cls.Name} должен содержать 'Async'");
            }
        }

        [TestMethod("Фильтр по имени метода")]
        public void FilterDemo_ByMethodName()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var testClass = assembly.GetTypes()
                .First(t => t.Name == "SlowTests");

            var allMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                .ToList();

            Console.WriteLine($"Всего методов в SlowTests: {allMethods.Count}");

            TestFilterDelegate filter = TestFilters.ByMethodName("Timeout");

            var filteredMethods = allMethods.ApplyFilter(testClass, filter).ToList();

            Console.WriteLine($"Методов с 'Timeout' в имени: {filteredMethods.Count}");

            foreach (var method in filteredMethods)
            {
                Console.WriteLine($"  - {method.Name}");
                Assert.IsTrue(method.Name.Contains("Timeout", StringComparison.OrdinalIgnoreCase),
                    $"Метод {method.Name} должен содержать 'Timeout'");
            }
        }


        [TestMethod("Фильтр параметризованных тестов")]
        public void FilterDemo_ParameterizedOnly()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var testClass = assembly.GetTypes()
                .First(t => t.Name == "ParameterizedTests");

            var allMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                .ToList();

            TestFilterDelegate filter = TestFilters.Parameterized();

            var filteredMethods = allMethods.ApplyFilter(testClass, filter).ToList();

            Console.WriteLine($"Параметризованных методов: {filteredMethods.Count}");

            foreach (var method in filteredMethods)
            {
                var dataRows = method.GetCustomAttributes<DataRowAttribute>().Count();
                Console.WriteLine($"  - {method.Name} ({dataRows} DataRow)");
                Assert.GreaterThan(dataRows, 0, 
                    $"Метод {method.Name} должен иметь DataRow атрибуты");
            }
        }

        [TestMethod("Фильтр тестов с Timeout")]
        public void FilterDemo_WithTimeout()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var testClass = assembly.GetTypes()
                .First(t => t.Name == "SlowTests");

            var allMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                .ToList();

            TestFilterDelegate filter = TestFilters.WithTimeout();

            var filteredMethods = allMethods.ApplyFilter(testClass, filter).ToList();

            Console.WriteLine($"Методов с Timeout: {filteredMethods.Count}");

            foreach (var method in filteredMethods)
            {
                var timeout = method.GetCustomAttribute<TimeoutAttribute>();
                Assert.IsNotNull(timeout, $"Метод {method.Name} должен иметь TimeoutAttribute");
                Console.WriteLine($"  - {method.Name} (timeout: {timeout.Milliseconds}ms)");
            }
        }

        [TestMethod("Комбинация фильтров через AND")]
        public void FilterDemo_CombineWithAnd()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allTestClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();

            TestFilterDelegate filter = TestFilters.And(
                TestFilters.ByCategory("Performance"),
                TestFilters.NotIgnored()
            );

            var filteredClasses = allTestClasses.ApplyClassFilter(filter).ToList();

            Console.WriteLine($"Классов Performance (не игнорируемых): {filteredClasses.Count}");

            foreach (var cls in filteredClasses)
            {
                var attr = cls.GetCustomAttribute<TestClassAttribute>();
                var ignore = cls.GetCustomAttribute<IgnoreAttribute>();
                
                Assert.AreEqual("Performance", attr?.Category);
                Assert.IsNull(ignore, $"Класс {cls.Name} не должен быть игнорируемым");
            }
        }

        [TestMethod("Комбинация фильтров через OR")]
        public void FilterDemo_CombineWithOr()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allTestClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();

            TestFilterDelegate filter = TestFilters.Or(
                TestFilters.ByCategory("Performance"),
                TestFilters.ByCategory("Advanced")
            );

            var filteredClasses = allTestClasses.ApplyClassFilter(filter).ToList();

            Console.WriteLine($"Классов Performance или Advanced: {filteredClasses.Count}");

            foreach (var cls in filteredClasses)
            {
                var attr = cls.GetCustomAttribute<TestClassAttribute>();
                bool valid = attr?.Category == "Performance" || attr?.Category == "Advanced";
                Assert.IsTrue(valid, 
                    $"Класс {cls.Name} должен иметь категорию Performance или Advanced");
            }
        }


        [TestMethod("Инверсия фильтра через NOT")]
        public void FilterDemo_NotFilter()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var testClass = assembly.GetTypes()
                .First(t => t.Name == "ParameterizedTests");

            var allMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                .ToList();

            TestFilterDelegate filter = TestFilters.Not(TestFilters.Parameterized());

            var filteredMethods = allMethods.ApplyFilter(testClass, filter).ToList();

            Console.WriteLine($"Не параметризованных методов: {filteredMethods.Count}");

            foreach (var method in filteredMethods)
            {
                var dataRows = method.GetCustomAttributes<DataRowAttribute>().Count();
                Assert.AreEqual(0, dataRows, 
                    $"Метод {method.Name} не должен иметь DataRow");
            }
        }


        [TestMethod("Fluent API для построения фильтров")]
        public void FilterDemo_FluentBuilder()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allTestClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();

            var filter = new TestFilterBuilder()
                .WithCategory("Performance")
                .ExcludeIgnored()
                .UseAndMode()
                .Build();

            var filteredClasses = allTestClasses.ApplyClassFilter(filter).ToList();

            Console.WriteLine($"Классов (Fluent API): {filteredClasses.Count}");

            foreach (var cls in filteredClasses)
            {
                var attr = cls.GetCustomAttribute<TestClassAttribute>();
                Console.WriteLine($"  - {cls.Name} (Category: {attr?.Category})");
            }
        }

        [TestMethod("Пользовательский фильтр через Custom")]
        public void FilterDemo_CustomFilter()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var testClass = assembly.GetTypes()
                .First(t => t.Name == "SlowTests");

            var allMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                .ToList();

            TestFilterDelegate filter = TestFilters.Custom(ctx => 
                ctx.MethodName.StartsWith("Slow_", StringComparison.Ordinal));

            var filteredMethods = allMethods.ApplyFilter(testClass, filter).ToList();

            Console.WriteLine($"Методов, начинающихся с 'Slow_': {filteredMethods.Count}");

            foreach (var method in filteredMethods)
            {
                Console.WriteLine($"  - {method.Name}");
                Assert.IsTrue(method.Name.StartsWith("Slow_"),
                    $"Метод {method.Name} должен начинаться с 'Slow_'");
            }
        }


        [TestMethod("Сложный составной фильтр")]
        public void FilterDemo_ComplexFilter()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allTestClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();


            TestFilterDelegate filter = TestFilters.And(
                TestFilters.Or(
                    TestFilters.ByCategory("Performance"),
                    TestFilters.ByClassName("Slow")
                ),
                TestFilters.NotIgnored()
            );

            var filteredClasses = allTestClasses.ApplyClassFilter(filter).ToList();

            Console.WriteLine($"Классов по сложному фильтру: {filteredClasses.Count}");

            foreach (var cls in filteredClasses)
            {
                var attr = cls.GetCustomAttribute<TestClassAttribute>();
                var ignore = cls.GetCustomAttribute<IgnoreAttribute>();
                
                bool matchesCategory = attr?.Category == "Performance";
                bool matchesName = cls.Name.Contains("Slow", StringComparison.OrdinalIgnoreCase);
                
                Console.WriteLine($"  - {cls.Name} (Category: {attr?.Category}, " +
                                $"MatchesName: {matchesName})");
                
                Assert.IsTrue(matchesCategory || matchesName, 
                    "Должна быть категория Performance или имя содержит Slow");
                Assert.IsNull(ignore, "Не должен быть игнорируемым");
            }
        }

        [TestMethod("Фильтр по нескольким категориям")]
        public void FilterDemo_MultipleCategories()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allTestClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();

            TestFilterDelegate filter = TestFilters.ByCategories(
                "Performance", "Advanced", "Events", "Filtering");

            var filteredClasses = allTestClasses.ApplyClassFilter(filter).ToList();

            Console.WriteLine($"Классов с указанными категориями: {filteredClasses.Count}");

            foreach (var cls in filteredClasses)
            {
                var attr = cls.GetCustomAttribute<TestClassAttribute>();
                Console.WriteLine($"  - {cls.Name} (Category: {attr?.Category})");
                
                var validCategories = new[] { "Performance", "Advanced", "Events", "Filtering" };
                Assert.IsTrue(validCategories.Contains(attr?.Category),
                    $"Категория {attr?.Category} должна быть в списке разрешённых");
            }
        }


        [TestMethod("Применение фильтра к методам класса")]
        public void FilterDemo_FilterMethodsInClass()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var testClass = assembly.GetTypes()
                .First(t => t.Name == "ParameterizedTests");

            var allMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                .ToList();

            Console.WriteLine($"\nВсего методов в {testClass.Name}: {allMethods.Count}");

            var parameterized = allMethods
                .ApplyFilter(testClass, TestFilters.Parameterized())
                .ToList();
            Console.WriteLine($"  Параметризованных: {parameterized.Count}");

            var withTimeout = allMethods
                .ApplyFilter(testClass, TestFilters.WithTimeout())
                .ToList();
            Console.WriteLine($"  С Timeout: {withTimeout.Count}");

            var expectsException = allMethods
                .ApplyFilter(testClass, TestFilters.ExpectsException())
                .ToList();
            Console.WriteLine($"  Ожидающих исключение: {expectsException.Count}");

            Assert.GreaterThan(allMethods.Count, 0, "Должны быть методы для фильтрации");
        }
    }
}