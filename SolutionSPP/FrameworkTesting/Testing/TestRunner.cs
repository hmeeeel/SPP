using FrameworkTesting.Assert;
using FrameworkTesting.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FrameworkTesting.Testing
{
    public class TestRunner
    {
        private TextWriter _output;

        public TestRunner()
        {
            _output = Console.Out;
        }

        public async Task<List<TestClassResult>> RunAllAsync(Assembly assembly)
        {
            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() is not null
                            && t.GetCustomAttribute<IgnoreAttribute>() is null)
                .OrderBy(t => t.Name)
                .ToList();

            _output.WriteLine($"  Запуск тестов из: {assembly.GetName().Name}");
            _output.WriteLine($"  Найдено классов: {testClasses.Count}");

            var allResults = new List<TestClassResult>();
            foreach (var cls in testClasses)
                allResults.Add(await RunClassAsync(cls));

            PrintSummary(allResults);
            return allResults;
        }

        public async Task<TestClassResult> RunClassAsync(Type testClass)
        {
            var classAttr = testClass.GetCustomAttribute<TestClassAttribute>();
            var classResult = new TestClassResult { ClassName = testClass.Name };

            _output.WriteLine($"  {testClass.Name}");

            bool isShared = testClass.GetCustomAttribute<SharedContextAttribute>() is not null; //!
            var setupMethod = FindMethodWithAttribute<TestSetupAttribute>(testClass);
            var teardownMethod = FindMethodWithAttribute<TestTeardownAttribute>(testClass);

            var testMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() is not null)
                .OrderBy(m => m.Name)
                .ToList();

            object? sharedContext = isShared ? CreateInstance(testClass) : null;

            foreach (var method in testMethods)
            {
                object? instance = isShared ? sharedContext : CreateInstance(testClass);
                if (instance is null)
                {
                    classResult.Results.Add(new TestMethodResult
                    {
                        ClassName = testClass.Name,
                        MethodName = method.Name,
                        DisplayName = method.Name,
                        Status = TestStatus.Error,
                        ErrorMessage = $"Не удалось создать экземпляр {testClass.Name}"
                    });
                    continue;
                }

                var result = await RunTestMethodAsync(instance, method, setupMethod, teardownMethod);
                classResult.Results.Add(result);
                _output.WriteLine($"   {result}");
            }

            _output.WriteLine();
            return classResult;
        }


        private async Task<TestMethodResult> RunTestMethodAsync(object instance, MethodInfo method, MethodInfo? setupMethod, MethodInfo? teardownMethod)
        {
            var methodAttr = method.GetCustomAttribute<TestMethodAttribute>()!;
            var ignoreAttr = method.GetCustomAttribute<IgnoreAttribute>();
            var expectedExAttr = method.GetCustomAttribute<ExpectedExceptionAttribute>();

            var displayName = method.Name;
            var startTime = DateTime.UtcNow;

            if (ignoreAttr is not null)
            {
                return new TestMethodResult
                {
                    ClassName = instance.GetType().Name,
                    MethodName = method.Name,
                    DisplayName = displayName,
                    Status = TestStatus.Skipped,
                    Duration = TimeSpan.Zero,
                };
            }

            // Setup
            try
            {
                if (setupMethod is not null)
                    await InvokeAsync(instance, setupMethod);
            }
            catch (Exception ex)
            {
                return ErrorResult(instance, method, displayName, startTime,
                    $"Setup упал: {ex.InnerException?.Message ?? ex.Message}");
            }

            // Тело 
            TestMethodResult result;
            try
            {
                await InvokeAsync(instance, method); // !!!!!

                if (expectedExAttr is not null)
                {
                    result = FailedResult(instance, method, displayName, startTime,
                        $"Ожидалось исключение {expectedExAttr.ExceptionType.Name}, но оно не было выброшено");
                }
                else
                {
                    result = new TestMethodResult
                    {
                        ClassName = instance.GetType().Name,
                        MethodName = method.Name,
                        DisplayName = displayName,
                        Status = TestStatus.Passed,
                        Duration = DateTime.UtcNow - startTime
                    };
                }
            }
            catch (Exception rawEx)
            {
                var ex = rawEx is TargetInvocationException tie ? tie.InnerException ?? tie : rawEx;

                if (expectedExAttr is not null)
                {
                    if (expectedExAttr.ExceptionType.IsInstanceOfType(ex))
                    {
                        if (expectedExAttr.MessageContains is not null &&
                            !ex.Message.Contains(expectedExAttr.MessageContains, StringComparison.OrdinalIgnoreCase))
                        {
                            result = FailedResult(instance, method, displayName, startTime,
                                $"Исключение {ex.GetType().Name} выброшено, но сообщение \"{ex.Message}\" " +
                                $"не содержит \"{expectedExAttr.MessageContains}\"");
                        }
                        else
                        {
                            result = new TestMethodResult
                            {
                                ClassName = instance.GetType().Name,
                                MethodName = method.Name,
                                DisplayName = displayName,
                                Status = TestStatus.Passed,
                                Duration = DateTime.UtcNow - startTime
                            };
                        }
                    }
                    else
                    {
                        result = FailedResult(instance, method, displayName, startTime,
                            $"Ожидалось {expectedExAttr.ExceptionType.Name}, но выброшено {ex.GetType().Name}: {ex.Message}");
                    }
                }
                else if (ex is AssertException afe)
                {
                    result = FailedResult(instance, method, displayName, startTime, afe.Message, afe);
                }
                else
                {
                    result = FailedResult(instance, method, displayName, startTime,
                        $"{ex.GetType().Name}: {ex.Message}", ex);
                }
            }

            try
            {
                if (teardownMethod is not null)
                    await InvokeAsync(instance, teardownMethod);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"   Teardown упал для {method.Name}: {ex.InnerException?.Message ?? ex.Message}");
            }

            return result;
        }


        private static object? CreateInstance(Type type)
        {
            try { return Activator.CreateInstance(type); }
            catch { return null; }
        }

        private static MethodInfo? FindMethodWithAttribute<TAttr>(Type type) where TAttr : Attribute =>
            type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.GetCustomAttribute<TAttr>() is not null);


        private static async Task InvokeAsync(object instance, MethodInfo method)
        {
            var result = method.Invoke(instance, null);
            if (result is Task task)
                await task;
        }

        private static TestMethodResult FailedResult(object instance, MethodInfo method,
            string displayName, DateTime start, string error, Exception? ex = null) =>
            new()
            {
                ClassName = instance.GetType().Name,
                MethodName = method.Name,
                DisplayName = displayName,
                Status = TestStatus.Failed,
                Duration = DateTime.UtcNow - start,
                ErrorMessage = error,
                Exception = ex
            };

        private static TestMethodResult ErrorResult(object instance, MethodInfo method,
            string displayName, DateTime start, string error) =>
            new()
            {
                ClassName = instance.GetType().Name,
                MethodName = method.Name,
                DisplayName = displayName,
                Status = TestStatus.Error,
                Duration = DateTime.UtcNow - start,
                ErrorMessage = error
            };

        private void PrintSummary(List<TestClassResult> results)
        {
            int total = results.Sum(r => r.Total);
            int passed = results.Sum(r => r.Passed);
            int failed = results.Sum(r => r.Failed);
            int skipped = results.Sum(r => r.Skipped);
            int errors = results.Sum(r => r.Errors);

            _output.WriteLine($"  ИТОГИ:");
            _output.WriteLine($"  Всего:    {total}");
            _output.WriteLine($"  Прошло: {passed}");
            _output.WriteLine($"  Упало:  {failed}");
            _output.WriteLine($"  Пропущено: {skipped}");
            _output.WriteLine($"  Ошибки: {errors}");
        }
    }
}