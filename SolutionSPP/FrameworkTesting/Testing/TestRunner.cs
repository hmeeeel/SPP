using FrameworkTesting.Assert;
using FrameworkTesting.Attributes;
using System.Diagnostics;
using System.Reflection;

namespace FrameworkTesting.Testing
{
    public class TestRunner
    {
        private readonly TextWriter _output;
        private readonly TestRunnerOptions _options;
        private readonly object _outputLock = new();

        private int _totalPassed;
        private int _totalFailed;
        private int _totalSkipped;
        private int _totalErrors;

        public TestRunner() : this(new TestRunnerOptions()) { }

        public TestRunner(TestRunnerOptions? options)
        {
            _output  = Console.Out;
            _options = options ?? new TestRunnerOptions();
        }

        private void SafeWriteLine(string text)
        {
            lock (_outputLock)
                _output.WriteLine($"[T{Environment.CurrentManagedThreadId:D2}] {text}");
        }

        public async Task<List<TestClassResult>> RunAllAsync(Assembly assembly)
        {
            _totalPassed = _totalFailed = _totalSkipped = _totalErrors = 0;

            var testClasses = DiscoverClasses(assembly);

            SafeWriteLine($"  Запуск тестов из: {assembly.GetName().Name}");
            SafeWriteLine($"  Найдено классов: {testClasses.Count}");

            var allResults = new List<TestClassResult>();
            using var semaphore = new SemaphoreSlim(_options.MaxDegreeOfParallelism);

            var tasks = testClasses.Select(cls => Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var classResult = await RunClassAsync(cls);
                    lock (allResults) allResults.Add(classResult);
                }
                finally { semaphore.Release(); }
            })).ToList();

            await Task.WhenAll(tasks);
            PrintSummary(allResults);
            return allResults;
        }



        public async Task<List<TestClassResult>> RunAllWithPoolAsync(
            Assembly assembly,
            Action<Action, string> enqueueToPool)
        {
            _totalPassed = _totalFailed = _totalSkipped = _totalErrors = 0;

            var testClasses = DiscoverClasses(assembly);

            SafeWriteLine($"  [DynPool] Запуск тестов из: {assembly.GetName().Name}");
            SafeWriteLine($"  [DynPool] Найдено классов: {testClasses.Count}");

            var allResults = new List<TestClassResult>();

            // Для каждого класса создаём TCS и отправляем работу в пул
            var tcsList = testClasses.Select(cls =>
            {
                var tcs = new TaskCompletionSource<TestClassResult>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                // enqueueToPool принимает Action - выполняется внутри Thread рабочего потока
                enqueueToPool(() =>
                {
                    try
                    {
                        // Синхронно ждём результата async-метода внутри потока пула
                        var result = RunClassAsync(cls).GetAwaiter().GetResult();
                        tcs.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }, cls.Name);

                return tcs.Task;
            }).ToList();

            var results = await Task.WhenAll(tcsList);

            foreach (var r in results)
                lock (allResults)
                    allResults.Add(r);

            PrintSummary(allResults);
            return allResults;
        }


        public async Task<TestClassResult> RunClassAsync(Type testClass)
        {
            var classAttr     = testClass.GetCustomAttribute<TestClassAttribute>()!;
            var categoryLabel = classAttr.Category is not null ? $" [{classAttr.Category}]" : "";
            var classResult   = new TestClassResult { ClassName = testClass.Name };

            SafeWriteLine($"  {testClass.Name}{categoryLabel}");

            bool isShared      = testClass.GetCustomAttribute<SharedContextAttribute>() is not null;
            var setupMethod    = FindMethodWithAttribute<TestSetupAttribute>(testClass);
            var teardownMethod = FindMethodWithAttribute<TestTeardownAttribute>(testClass);

            var testMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() is not null)
                .OrderBy(m => m.Name)
                .ToList();

            object? sharedContext = isShared ? CreateInstance(testClass) : null;
            bool parallelMethods  = _options.ParallelizeMethods && !isShared;

            var classWatch = Stopwatch.StartNew();
            if (parallelMethods)
                await RunMethodsParallelAsync(testClass, testMethods,
                                              setupMethod, teardownMethod, classResult);
            else
                await RunMethodsSequentialAsync(testClass, testMethods, isShared, sharedContext,
                                                setupMethod, teardownMethod, classResult);
            classWatch.Stop();
            classResult.Duration = classWatch.Elapsed;

            SafeWriteLine($"   {classResult}");
            SafeWriteLine(string.Empty);
            return classResult;
        }

        private async Task RunMethodsSequentialAsync(
            Type testClass, List<MethodInfo> testMethods,
            bool isShared, object? sharedContext,
            MethodInfo? setupMethod, MethodInfo? teardownMethod,
            TestClassResult classResult)
        {
            foreach (var method in testMethods)
            {
                var dataRows = method.GetCustomAttributes<DataRowAttribute>().ToList();

                if (dataRows.Count > 0)
                {
                    foreach (var row in dataRows)
                    {
                        object? instance = isShared ? sharedContext : CreateInstance(testClass);
                        if (instance is null)
                        {
                            var err = CreateInstanceErrorResult(testClass.Name, method.Name);
                            classResult.Results.Add(err);
                            IncrementCounter(err.Status);
                            continue;
                        }
                        var dn     = row.DisplayName ?? BuildParameterizedDisplayName(method, row.Values);
                        var result = await RunParameterizedTestAsync(
                            instance, method, row.Values, dn, setupMethod, teardownMethod);
                        classResult.Results.Add(result);
                        IncrementCounter(result.Status);
                        SafeWriteLine($"   {result}");
                    }
                }
                else
                {
                    object? instance = isShared ? sharedContext : CreateInstance(testClass);
                    if (instance is null)
                    {
                        var err = CreateInstanceErrorResult(testClass.Name, method.Name);
                        classResult.Results.Add(err);
                        IncrementCounter(err.Status);
                        continue;
                    }
                    var result = await RunTestMethodAsync(instance, method, setupMethod, teardownMethod);
                    classResult.Results.Add(result);
                    IncrementCounter(result.Status);
                    SafeWriteLine($"   {result}");
                }
            }
        }

        private async Task RunMethodsParallelAsync(
            Type testClass, List<MethodInfo> testMethods,
            MethodInfo? setupMethod, MethodInfo? teardownMethod,
            TestClassResult classResult)
        {
            using var semaphore   = new SemaphoreSlim(_options.MaxDegreeOfParallelism);
            var methodResults = new List<TestMethodResult>();
            var tasks         = new List<Task>();

            foreach (var method in testMethods)
            {
                var dataRows = method.GetCustomAttributes<DataRowAttribute>().ToList();

                if (dataRows.Count > 0)
                {
                    foreach (var row in dataRows)
                    {
                        var cr = row; var cm = method;
                        tasks.Add(Task.Run(async () =>
                        {
                            await semaphore.WaitAsync();
                            try
                            {
                                object? inst = CreateInstance(testClass);
                                if (inst is null)
                                {
                                    var e = CreateInstanceErrorResult(testClass.Name, cm.Name);
                                    lock (methodResults) methodResults.Add(e);
                                    IncrementCounter(e.Status); return;
                                }
                                var dn = cr.DisplayName
                                    ?? BuildParameterizedDisplayName(cm, cr.Values);
                                var r = await RunParameterizedTestAsync(
                                    inst, cm, cr.Values, dn, setupMethod, teardownMethod);
                                lock (methodResults) methodResults.Add(r);
                                IncrementCounter(r.Status);
                                SafeWriteLine($"   {r}");
                            }
                            finally { semaphore.Release(); }
                        }));
                    }
                }
                else
                {
                    var cm = method;
                    tasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            object? inst = CreateInstance(testClass);
                            if (inst is null)
                            {
                                var e = CreateInstanceErrorResult(testClass.Name, cm.Name);
                                lock (methodResults) methodResults.Add(e);
                                IncrementCounter(e.Status); return;
                            }
                            var r = await RunTestMethodAsync(inst, cm, setupMethod, teardownMethod);
                            lock (methodResults) methodResults.Add(r);
                            IncrementCounter(r.Status);
                            SafeWriteLine($"   {r}");
                        }
                        finally { semaphore.Release(); }
                    }));
                }
            }

            await Task.WhenAll(tasks);
            classResult.Results.AddRange(methodResults);
        }

        private async Task<TestMethodResult> RunTestMethodAsync(
            object instance, MethodInfo method,
            MethodInfo? setupMethod, MethodInfo? teardownMethod)
        {
            var methodAttr     = method.GetCustomAttribute<TestMethodAttribute>()!;
            var ignoreAttr     = method.GetCustomAttribute<IgnoreAttribute>();
            var expectedExAttr = method.GetCustomAttribute<ExpectedExceptionAttribute>();
            var timeoutAttr    = method.GetCustomAttribute<TimeoutAttribute>();
            var displayName    = methodAttr.DisplayName ?? method.Name;
            var startTime      = DateTime.UtcNow;

            if (ignoreAttr is not null)
                return new TestMethodResult
                {
                    ClassName   = instance.GetType().Name, MethodName = method.Name,
                    DisplayName = displayName, Status = TestStatus.Skipped, Duration = TimeSpan.Zero
                };

            try { if (setupMethod is not null) await InvokeAsync(instance, setupMethod, null); }
            catch (Exception ex)
            {
                return ErrorResult(instance, method, displayName, startTime,
                    $"Setup упал: {ex.InnerException?.Message ?? ex.Message}");
            }

            var result = await RunWithOptionalTimeoutAsync(
                instance, method, null, displayName, startTime, expectedExAttr, timeoutAttr);

            try { if (teardownMethod is not null) await InvokeAsync(instance, teardownMethod, null); }
            catch (Exception ex)
            {
                SafeWriteLine($"   Teardown упал для {method.Name}: " +
                              $"{ex.InnerException?.Message ?? ex.Message}");
            }

            return result;
        }

        private async Task<TestMethodResult> RunParameterizedTestAsync(
            object instance, MethodInfo method, object?[] args, string displayName,
            MethodInfo? setupMethod, MethodInfo? teardownMethod)
        {
            var ignoreAttr     = method.GetCustomAttribute<IgnoreAttribute>();
            var expectedExAttr = method.GetCustomAttribute<ExpectedExceptionAttribute>();
            var timeoutAttr    = method.GetCustomAttribute<TimeoutAttribute>();
            var startTime      = DateTime.UtcNow;

            if (ignoreAttr is not null)
                return new TestMethodResult
                {
                    ClassName   = instance.GetType().Name, MethodName = method.Name,
                    DisplayName = displayName, Status = TestStatus.Skipped, Duration = TimeSpan.Zero
                };

            try { if (setupMethod is not null) await InvokeAsync(instance, setupMethod, null); }
            catch (Exception ex)
            {
                return ErrorResult(instance, method, displayName, startTime,
                    $"Setup упал: {ex.InnerException?.Message ?? ex.Message}");
            }

            var result = await RunWithOptionalTimeoutAsync(
                instance, method, args, displayName, startTime, expectedExAttr, timeoutAttr);

            try { if (teardownMethod is not null) await InvokeAsync(instance, teardownMethod, null); }
            catch (Exception ex)
            {
                SafeWriteLine($"   Teardown упал для {method.Name}: " +
                              $"{ex.InnerException?.Message ?? ex.Message}");
            }

            return result;
        }

        private async Task<TestMethodResult> RunWithOptionalTimeoutAsync(
            object instance, MethodInfo method, object?[]? args,
            string displayName, DateTime startTime,
            ExpectedExceptionAttribute? expectedExAttr, TimeoutAttribute? timeoutAttr)
        {
            if (timeoutAttr is not null)
            {
                using var cts   = new CancellationTokenSource(timeoutAttr.Milliseconds);
                CancellationToken token = cts.Token;

                Task testTask = Task.Run(async () =>
                {
                    token.ThrowIfCancellationRequested();
                    await InvokeAsync(instance, method, args, token);
                }, token);

                try
                {
                    await testTask.WaitAsync(token);
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    _ = testTask.ContinueWith(
                        static t => { _ = t.Exception; },
                        TaskContinuationOptions.OnlyOnFaulted);

                    return FailedResult(instance, method, displayName, startTime,
                        $"Тест превысил лимит времени: {timeoutAttr.Milliseconds}мс");
                }
                catch (Exception rawEx)
                {
                    return HandleTestException(rawEx, instance, method,
                                               displayName, startTime, expectedExAttr);
                }
            }
            else
            {
                try { await InvokeAsync(instance, method, args); }
                catch (Exception rawEx)
                {
                    return HandleTestException(rawEx, instance, method,
                                               displayName, startTime, expectedExAttr);
                }
            }

            if (expectedExAttr is not null)
                return FailedResult(instance, method, displayName, startTime,
                    $"Ожидалось исключение {expectedExAttr.ExceptionType.Name}, " +
                    $"но оно не было выброшено");

            return new TestMethodResult
            {
                ClassName   = instance.GetType().Name,
                MethodName  = method.Name,
                DisplayName = displayName,
                Status      = TestStatus.Passed,
                Duration    = DateTime.UtcNow - startTime
            };
        }

        private static List<Type> DiscoverClasses(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<TestClassAttribute>() is not null
                                && t.GetCustomAttribute<IgnoreAttribute>() is null)
                    .OrderBy(t => t.Name)
                    .ToList();
            }
            catch (ReflectionTypeLoadException ex)
            {
                throw new TestFrameworkException(
                    $"Не удалось загрузить типы из '{assembly.GetName().Name}': " +
                    $"{ex.LoaderExceptions.FirstOrDefault()?.Message}", ex);
            }
        }

        private static TestMethodResult HandleTestException(
            Exception rawEx, object instance, MethodInfo method,
            string displayName, DateTime startTime,
            ExpectedExceptionAttribute? expectedExAttr)
        {
            var ex = rawEx is TargetInvocationException tie ? tie.InnerException ?? tie : rawEx;

            if (expectedExAttr is not null)
            {
                if (expectedExAttr.ExceptionType.IsInstanceOfType(ex))
                {
                    if (expectedExAttr.MessageContains is not null &&
                        !ex.Message.Contains(expectedExAttr.MessageContains,
                                             StringComparison.OrdinalIgnoreCase))
                    {
                        return FailedResult(instance, method, displayName, startTime,
                            $"Исключение {ex.GetType().Name} выброшено, " +
                            $"но сообщение \"{ex.Message}\" " +
                            $"не содержит \"{expectedExAttr.MessageContains}\"");
                    }
                    return new TestMethodResult
                    {
                        ClassName   = instance.GetType().Name,
                        MethodName  = method.Name,
                        DisplayName = displayName,
                        Status      = TestStatus.Passed,
                        Duration    = DateTime.UtcNow - startTime
                    };
                }
                return FailedResult(instance, method, displayName, startTime,
                    $"Ожидалось {expectedExAttr.ExceptionType.Name}, " +
                    $"но выброшено {ex.GetType().Name}: {ex.Message}");
            }

            if (ex is AssertException afe)
                return FailedResult(instance, method, displayName, startTime, afe.Message, afe);

            return FailedResult(instance, method, displayName, startTime,
                $"{ex.GetType().Name}: {ex.Message}", ex);
        }

        private void IncrementCounter(TestStatus status)
        {
            switch (status)
            {
                case TestStatus.Passed:  Interlocked.Increment(ref _totalPassed);  break;
                case TestStatus.Failed:  Interlocked.Increment(ref _totalFailed);  break;
                case TestStatus.Skipped: Interlocked.Increment(ref _totalSkipped); break;
                case TestStatus.Error:   Interlocked.Increment(ref _totalErrors);  break;
            }
        }

        private static object? CreateInstance(Type type)
        {
            try { return Activator.CreateInstance(type); }
            catch { return null; }
        }

        private static MethodInfo? FindMethodWithAttribute<TAttr>(Type type)
            where TAttr : Attribute =>
            type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.GetCustomAttribute<TAttr>() is not null);

        private static async Task InvokeAsync(
            object instance, MethodInfo method, object?[]? args,
            CancellationToken cancellationToken = default)
        {
            var parameters = method.GetParameters();

            if (parameters.Length > 0 &&
                parameters[^1].ParameterType == typeof(CancellationToken))
            {
                int originalLength  = args?.Length ?? 0;
                var extendedArgs    = new object?[originalLength + 1];
                if (args is not null) Array.Copy(args, extendedArgs, originalLength);
                extendedArgs[^1] = cancellationToken;
                args = extendedArgs;
            }

            var result = method.Invoke(instance, args);
            if (result is Task task) await task;
        }

        private static TestMethodResult FailedResult(
            object instance, MethodInfo method, string displayName,
            DateTime start, string error, Exception? ex = null) => new()
        {
            ClassName    = instance.GetType().Name,
            MethodName   = method.Name,
            DisplayName  = displayName,
            Status       = TestStatus.Failed,
            Duration     = DateTime.UtcNow - start,
            ErrorMessage = error,
            Exception    = ex
        };

        private static TestMethodResult ErrorResult(
            object instance, MethodInfo method, string displayName,
            DateTime start, string error) => new()
        {
            ClassName    = instance.GetType().Name,
            MethodName   = method.Name,
            DisplayName  = displayName,
            Status       = TestStatus.Error,
            Duration     = DateTime.UtcNow - start,
            ErrorMessage = error
        };

        private static TestMethodResult CreateInstanceErrorResult(
            string className, string methodName) => new()
        {
            ClassName    = className,
            MethodName   = methodName,
            DisplayName  = methodName,
            Status       = TestStatus.Error,
            ErrorMessage = $"Не удалось создать экземпляр {className}"
        };

        private static string BuildParameterizedDisplayName(MethodInfo method, object?[] values)
        {
            var args = string.Join(", ", values.Select(v => v?.ToString() ?? "null"));
            return $"{method.Name}({args})";
        }

        private void PrintSummary(List<TestClassResult> allResults)
        {
            int total     = _totalPassed + _totalFailed + _totalSkipped + _totalErrors;
            var totalTime = TimeSpan.FromTicks(allResults.Sum(r => r.Duration.Ticks));

            SafeWriteLine("  ИТОГИ:");
            SafeWriteLine($"  Всего:     {total}");
            SafeWriteLine($"  Прошло:    {_totalPassed}");
            SafeWriteLine($"  Упало:     {_totalFailed}");
            SafeWriteLine($"  Пропущено: {_totalSkipped}");
            SafeWriteLine($"  Ошибки:    {_totalErrors}");
            SafeWriteLine($"  Время:     {TestMethodResult.FormatMs(totalTime)}");
        }
    }
}