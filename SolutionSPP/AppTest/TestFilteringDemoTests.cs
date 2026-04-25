/*using FrameworkTesting.Assert;
using FrameworkTesting.Attributes;
using FrameworkTesting.Filtering;
using System.Reflection;

namespace AppTest.Tests
{
    /// <summary>
    /// Тесты для демонстрации фильтрации тестов с помощью делегатов.
    /// Требование: Механизм выборочного запуска тестов на основе свойств
    /// (категорий, приоритетов, автора) с помощью делегатов.
    /// </summary>
    [TestClass(Category = "Filtering", Description = "Демонстрация фильтрации через делегаты")]
    public class TestFilteringDemoTests
    {
        /// <summary>
        /// Демонстрация базовой фильтрации по категории.
        /// </summary>
        [TestMethod("Фильтр по категории")]
        public void FilterDemo_ByCategory()
        {
            // Получаем все тестовые классы из текущей сборки
            var assembly = Assembly.GetExecutingAssembly();
            var allTestClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();

            Console.WriteLine($"Всего тестовых классов: {allTestClasses.Count}");

            // Создаём фильтр для категории "Performance"
            TestFilterDelegate filter = TestFilters.ByCategory("Performance");

            // Применяем фильтр
            var filteredClasses = allTestClasses.ApplyClassFilter(filter).ToList();

            Console.WriteLine($"Классов с категорией 'Performance': {filteredClasses.Count}");

            // Проверяем, что фильтр работает
            foreach (var cls in filteredClasses)
            {
                var attr = cls.GetCustomAttribute<TestClassAttribute>();
                Assert.AreEqual("Performance", attr?.Category, 
                    $"Класс {cls.Name} должен иметь категорию Performance");
            }
        }

        /// <summary>
        /// Демонстрация фильтрации по имени класса.
        /// </summary>
        [TestMethod("Фильтр по имени класса")]
        public void FilterDemo_ByClassName()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allTestClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();

            // Фильтр: классы, содержащие "Async" в имени
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

        /// <summary>
        /// Демонстрация фильтрации по имени метода.
        /// </summary>
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

            // Фильтр: методы с "Timeout" в имени
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

        /// <summary>
        /// Демонстрация фильтрации параметризованных тестов.
        /// </summary>
        [TestMethod("Фильтр параметризованных тестов")]
        public void FilterDemo_ParameterizedOnly()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var testClass = assembly.GetTypes()
                .First(t => t.Name == "ParameterizedTests");

            var allMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                .ToList();

            // Фильтр: только параметризованные тесты
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

        /// <summary>
        /// Демонстрация фильтрации тестов с Timeout.
        /// </summary>
        [TestMethod("Фильтр тестов с Timeout")]
        public void FilterDemo_WithTimeout()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var testClass = assembly.GetTypes()
                .First(t => t.Name == "SlowTests");

            var allMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                .ToList();

            // Фильтр: только тесты с атрибутом Timeout
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

        /// <summary>
        /// Демонстрация комбинации фильтров через AND.
        /// </summary>
        [TestMethod("Комбинация фильтров через AND")]
        public void FilterDemo_CombineWithAnd()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allTestClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();

            // Комбинированный фильтр: категория "Performance" И не игнорируемые
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

        /// <summary>
        /// Демонстрация комбинации фильтров через OR.
        /// </summary>
        [TestMethod("Комбинация фильтров через OR")]
        public void FilterDemo_CombineWithOr()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allTestClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();

            // Комбинированный фильтр: категория "Performance" ИЛИ "Advanced"
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

        /// <summary>
        /// Демонстрация инверсии фильтра (NOT).
        /// </summary>
        [TestMethod("Инверсия фильтра через NOT")]
        public void FilterDemo_NotFilter()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var testClass = assembly.GetTypes()
                .First(t => t.Name == "ParameterizedTests");

            var allMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                .ToList();

            // Инверсия: НЕ параметризованные тесты
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

        /// <summary>
        /// Демонстрация Fluent API для построения фильтров.
        /// </summary>
        [TestMethod("Fluent API для построения фильтров")]
        public void FilterDemo_FluentBuilder()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allTestClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();

            // Построение фильтра через Fluent API
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

        /// <summary>
        /// Демонстрация пользовательского фильтра.
        /// </summary>
        [TestMethod("Пользовательский фильтр через Custom")]
        public void FilterDemo_CustomFilter()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var testClass = assembly.GetTypes()
                .First(t => t.Name == "SlowTests");

            var allMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                .ToList();

            // Пользовательский фильтр: методы, начинающиеся с "Slow_"
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

        /// <summary>
        /// Демонстрация сложного составного фильтра.
        /// </summary>
        [TestMethod("Сложный составной фильтр")]
        public void FilterDemo_ComplexFilter()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allTestClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();

            // Сложный фильтр:
            // (Категория = "Performance" ИЛИ имя содержит "Slow") И НЕ игнорируемые
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

        /// <summary>
        /// Демонстрация фильтрации по нескольким категориям.
        /// </summary>
        [TestMethod("Фильтр по нескольким категориям")]
        public void FilterDemo_MultipleCategories()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var allTestClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();

            // Фильтр: категории Performance, Advanced, Events, Filtering
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

        /// <summary>
        /// Демонстрация применения фильтра к методам конкретного класса.
        /// </summary>
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

            // Фильтр 1: Параметризованные
            var parameterized = allMethods
                .ApplyFilter(testClass, TestFilters.Parameterized())
                .ToList();
            Console.WriteLine($"  Параметризованных: {parameterized.Count}");

            // Фильтр 2: С Timeout
            var withTimeout = allMethods
                .ApplyFilter(testClass, TestFilters.WithTimeout())
                .ToList();
            Console.WriteLine($"  С Timeout: {withTimeout.Count}");

            // Фильтр 3: Ожидающие исключение
            var expectsException = allMethods
                .ApplyFilter(testClass, TestFilters.ExpectsException())
                .ToList();
            Console.WriteLine($"  Ожидающих исключение: {expectsException.Count}");

            Assert.GreaterThan(allMethods.Count, 0, "Должны быть методы для фильтрации");
        }
    }
}*/