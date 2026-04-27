﻿using CustomThreadPool;
using CustomThreadPool.Events;
using FrameworkTesting.Testing;
using FrameworkTesting.DataProviders;
using FrameworkTesting.Filtering;
using FrameworkTesting.Attributes;
using System.Diagnostics;
using System.Reflection;


static void DemonstrateIterators()
{
    Console.WriteLine("*******************************************************");
    Console.WriteLine("  1. ПРОСТОЙ ИТЕРАТОР: Граничные значения возраста");
    Console.WriteLine("*******************************************************");
    
    var ageData = TestDataProviders.AgeBoundaryValues().Take(5);
    int count = 0;
    foreach (var testCase in ageData)
    {
        count++;
        Console.WriteLine($"  Тест-кейс {count}: Age={testCase[0]}, Expected={testCase[1]}");
    }
    Console.WriteLine($"   {count} тест-кейсов");
    Console.WriteLine();
    
    Console.WriteLine("*******************************************************");
    Console.WriteLine("  2. КОМБИНАТОРНЫЙ ИТЕРАТОР: Age × Income");
    Console.WriteLine("*******************************************************");
    
    var combinations = TestDataProviders.AgeCombined().Take(6);
    count = 0;
    foreach (var testCase in combinations)
    {
        count++;
        Console.WriteLine($"  Комбинация {count}: Age={testCase[0]}, Income={testCase[1]}, " +
                         $"ShouldApprove={testCase[2]}");
    }
    Console.WriteLine($"  {count}");
    Console.WriteLine();
    
    Console.WriteLine("*******************************************************");
    Console.WriteLine("  3. БЕСКОНЕЧНЫЙ ИТЕРАТОР: Числа Фибоначчи");
    Console.WriteLine("*******************************************************");
    
    var fibonacci = TestDataProviders.RandomScenariosInfinite().Take(5);
    count = 0;
    foreach (var scenario in fibonacci)
    {
        count++;
        Console.WriteLine($"  Сценарий {count}: Age={scenario[0]}, Income={scenario[1]}, " +
                         $"Debt={scenario[2]}");
    }
    Console.WriteLine($"   {count}");
    Console.WriteLine();
    
    Console.WriteLine("*******************************************************");
    Console.WriteLine("  4. ФИЛЬТРУЮЩИЙ ИТЕРАТОР: Только положительные доходы");
    Console.WriteLine("*******************************************************");
    
    var positiveIncomes = TestDataProviders.PositiveIncomeCases();
    count = 0;
    foreach (var testCase in positiveIncomes)
    {
        count++;
        Console.WriteLine($"  Положительный доход {count}: {testCase[0]:C}");
    }
    Console.WriteLine($"   {count}");
    Console.WriteLine();
    
    Console.WriteLine();
}

static async Task DemonstrateEvents()
{
    Console.WriteLine("*******************************************************");
    Console.WriteLine("  СОБЫТИЯ: Отслеживание жизненного цикла пула");
    Console.WriteLine("*******************************************************");
    Console.WriteLine();
    
    var eventLog = new List<string>();
    var lockObj = new object();
    
    var aggregator = new PoolEventAggregator();
    
    var options = new ThreadPoolOptions
    {
        MinThreads = 2,
        MaxThreads = 4,
        IdleTimeoutMs = 1000
    };
    
    using var pool = new DynamicThreadPoolWithEvents(options);
    
    Console.WriteLine("  Подписка на события:");
    
    // 1. Общее событие
    pool.LifecycleEvent += (sender, e) =>
    {
        lock (lockObj)
        {
            eventLog.Add($"[{e.Timestamp:HH:mm:ss.fff}] {e.EventType}");
        }
    };
    Console.WriteLine("   LifecycleEvent - все события пула");
    
    // 2. Специализированные события
    pool.ThreadCreatedEvent += (sender, e) =>
    {
        lock (lockObj)
        {
            Console.WriteLine($"    Создан поток: T{e.ThreadId}");
        }
    };
    Console.WriteLine("    ThreadCreatedEvent - создание потоков");
    
    pool.ThreadTerminatedEvent += (sender, e) =>
    {
        lock (lockObj)
        {
            Console.WriteLine($"    Завершён поток: T{e.ThreadId}");
        }
    };
    Console.WriteLine("    ThreadTerminatedEvent - завершение потоков");
    
    pool.PoolScalingEvent += (sender, e) =>
    {
        lock (lockObj)
        {
            Console.WriteLine($"    Масштабирование: {e.EventType} - {e.Message}");
        }
    };
    Console.WriteLine("    PoolScalingEvent - масштабирование");
    
    pool.ErrorEvent += (sender, e) =>
    {
        lock (lockObj)
        {
            Console.WriteLine($"     Ошибка: {e.EventType} - {e.Message}");
        }
    };
    Console.WriteLine("    ErrorEvent - ошибки в задачах");
    
    pool.LifecycleEvent += aggregator.OnLifecycleEvent;
    Console.WriteLine("    PoolEventAggregator - сбор статистики");
    
    Console.WriteLine();
    Console.WriteLine("  Выполнение задач:");
    
    // Нормальные задачи
    for (int i = 0; i < 5; i++)
    {
        pool.Enqueue(() => Thread.Sleep(200), $"Task{i}");
        Console.WriteLine($"    Поставлена в очередь: Task{i}");
    }
    
    // Задача с ошибкой
    pool.Enqueue(() => throw new InvalidOperationException("Тестовая ошибка"), "ErrorTask");
    Console.WriteLine($"    Поставлена в очередь: ErrorTask (с ошибкой)");
    
    await Task.Delay(2000);
    
    Console.WriteLine();
    Console.WriteLine("*******************************************************");
    Console.WriteLine("  ИТОГИ СОБЫТИЙ:");
    Console.WriteLine("*******************************************************");
    Console.WriteLine(aggregator.GetSummary());
    
    lock (lockObj)
    {
        Console.WriteLine($"  Всего событий зафиксировано: {eventLog.Count}");
        
        var eventTypes = eventLog.Select(e => e.Split(']')[1].Trim())
                                 .Distinct()
                                 .OrderBy(x => x);
        Console.WriteLine($"  Уникальных типов событий: {eventTypes.Count()}");
        foreach (var type in eventTypes)
        {
            int count = eventLog.Count(e => e.Contains(type));
            Console.WriteLine($"    - {type}: {count}");
        }
    }
    
    Console.WriteLine();
    Console.WriteLine();
    
    pool.Shutdown(2000);
}

static void DemonstrateFiltering(Assembly testAssembly)
{
    Console.WriteLine("*******************************************************");
    Console.WriteLine("  ДЕЛЕГАТЫ: Фильтрация тестов");
    Console.WriteLine("*******************************************************");
    Console.WriteLine();
    
    var allTestClasses = testAssembly.GetTypes()
        .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
        .ToList();
    
    Console.WriteLine($"  Всего тестовых классов в сборке: {allTestClasses.Count}");
    Console.WriteLine();

    Console.WriteLine("   ФИЛЬТР ПО КАТЕГОРИИ: 'Performance'");
    TestFilterDelegate filter1 = TestFilters.ByCategory("Performance");
    var performance = allTestClasses.ApplyClassFilter(filter1).ToList();
    Console.WriteLine($"    Найдено классов: {performance.Count}");
    foreach (var cls in performance)
    {
        var attr = cls.GetCustomAttribute<TestClassAttribute>();
        Console.WriteLine($"    - {cls.Name} [{attr?.Category}]");
    }
    Console.WriteLine();
    
    Console.WriteLine("   ФИЛЬТР ПО ИМЕНИ КЛАССА: содержит 'Advanced'");
    TestFilterDelegate filter2 = TestFilters.ByClassName("Advanced");
    var advanced = allTestClasses.ApplyClassFilter(filter2).ToList();
    Console.WriteLine($"  ✓ Найдено классов: {advanced.Count}");
    foreach (var cls in advanced)
    {
        Console.WriteLine($"    - {cls.Name}");
    }
    Console.WriteLine();

    Console.WriteLine("  КОМБИНАЦИЯ AND: Performance И не игнорируемые");
    TestFilterDelegate filter3 = TestFilters.And(
        TestFilters.ByCategory("Performance"),
        TestFilters.NotIgnored()
    );
    var combined = allTestClasses.ApplyClassFilter(filter3).ToList();
    Console.WriteLine($"   Найдено классов: {combined.Count}");
    Console.WriteLine();
    
    Console.WriteLine("   КОМБИНАЦИЯ OR: Performance ИЛИ Advanced ИЛИ Events");
    TestFilterDelegate filter4 = TestFilters.Or(
        TestFilters.Or(
            TestFilters.ByCategory("Performance"),
            TestFilters.ByCategory("Advanced")
        ),
        TestFilters.ByCategory("Events")
    );
    var orFiltered = allTestClasses.ApplyClassFilter(filter4).ToList();
    Console.WriteLine($"   Найдено классов: {orFiltered.Count}");
    foreach (var cls in orFiltered)
    {
        var attr = cls.GetCustomAttribute<TestClassAttribute>();
        Console.WriteLine($"    - {cls.Name} [{attr?.Category}]");
    }
    Console.WriteLine();
    
    Console.WriteLine("   ИНВЕРСИЯ NOT: НЕ Performance");
    TestFilterDelegate filter5 = TestFilters.Not(TestFilters.ByCategory("Performance"));
    var notPerformance = allTestClasses.ApplyClassFilter(filter5).ToList();
    Console.WriteLine($"   Найдено классов: {notPerformance.Count}");
    Console.WriteLine();
    
    Console.WriteLine("  FLUENT API: Множественные условия");
    var fluentFilter = new TestFilterBuilder()
        .WithCategories("Performance", "Advanced", "Events")
        .ExcludeIgnored()
        .UseOrMode()
        .Build();
    var fluent = allTestClasses.ApplyClassFilter(fluentFilter).ToList();
    Console.WriteLine($"   Найдено классов: {fluent.Count}");
    Console.WriteLine();
    
    Console.WriteLine("  ПОЛЬЗОВАТЕЛЬСКИЙ ФИЛЬТР: Имя класса длиннее 15 символов");
    TestFilterDelegate customFilter = TestFilters.Custom(ctx => 
        ctx.ClassName.Length > 15);
    var custom = allTestClasses.ApplyClassFilter(customFilter).ToList();
    Console.WriteLine($"   Найдено классов: {custom.Count}");
    foreach (var cls in custom)
    {
        Console.WriteLine($"    - {cls.Name} (длина: {cls.Name.Length})");
    }
    Console.WriteLine();
     
    var testClass = allTestClasses.FirstOrDefault(t => t.Name == "SlowTests");
    if (testClass != null)
    {
        Console.WriteLine("  ФИЛЬТРАЦИЯ МЕТОДОВ: Методы с Timeout в SlowTests");
        var allMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
            .ToList();
        
        TestFilterDelegate methodFilter = TestFilters.WithTimeout();
        var timeoutMethods = allMethods.ApplyFilter(testClass, methodFilter).ToList();
        
        Console.WriteLine($"    Всего методов: {allMethods.Count}");
        Console.WriteLine($"    С Timeout: {timeoutMethods.Count}");
        foreach (var method in timeoutMethods.Take(3))
        {
            var timeout = method.GetCustomAttribute<TimeoutAttribute>();
            Console.WriteLine($"    - {method.Name} (timeout: {timeout?.Milliseconds}ms)");
        }
    }
    
    Console.WriteLine();
    Console.WriteLine();
}


try
{
    string? assemblyPath = args.FirstOrDefault(a => !a.StartsWith("--"));
    Assembly testAssembly;
    if (assemblyPath is not null)
    {
        Console.WriteLine($"Загрузка сборки: {assemblyPath}");
        testAssembly = Assembly.LoadFrom(assemblyPath);
    }
    else
    {
        testAssembly = Assembly.Load("AppTest");
    }
    Console.WriteLine();
    
    Console.WriteLine("*******************************************************");
    Console.WriteLine("     ДЕМОНСТРАЦИЯ ТРЕХ НОВЫХ ТРЕБОВАНИЙ               ");
    Console.WriteLine("  1. Итераторы (yield return)                         ");
    Console.WriteLine("  2. События (events)                                 ");
    Console.WriteLine("  3. Фильтрация делегатами (delegates)                ");
    Console.WriteLine("*******************************************************");
    Console.WriteLine();
    Console.WriteLine("Нажмите любую клавишу для начала демонстрации...");
    Console.ReadKey(true);
    Console.Clear();
    

    DemonstrateIterators();
    Console.WriteLine("Нажмите любую клавишу для перехода к следующей демонстрации...");
    Console.ReadKey(true);
    Console.Clear();
    
    await DemonstrateEvents();
    Console.WriteLine("Нажмите любую клавишу для перехода к следующей демонстрации...");
    Console.ReadKey(true);
    Console.Clear();
    

    DemonstrateFiltering(testAssembly);
    Console.WriteLine("Нажмите любую клавишу для запуска тестов...");
    Console.ReadKey(true);
    Console.Clear();
    
    Console.WriteLine("*******************************************************");
    Console.WriteLine("                ПОСЛЕДОВАТЕЛЬНЫЙ ЗАПУСК                ");
    Console.WriteLine("*******************************************************");

    var optionsSeq = new TestRunnerOptions { MaxDegreeOfParallelism = 1, ParallelizeMethods = false };
    var runnerSeq  = new TestRunner(optionsSeq);

    var sw1      = Stopwatch.StartNew();
    var results1 = await runnerSeq.RunAllAsync(testAssembly);
    sw1.Stop();

    Console.WriteLine($"  Время (последовательно): {sw1.ElapsedMilliseconds} мс");
    Console.WriteLine();
    
    Console.WriteLine("*******************************************************");
    Console.WriteLine("           ПАРАЛЛЕЛЬНЫЙ ЗАПУСК (Task.Run)              ");
    Console.WriteLine("*******************************************************");

    var optionsPar = new TestRunnerOptions { MaxDegreeOfParallelism = 4, ParallelizeMethods = true };
    var runnerPar  = new TestRunner(optionsPar);

    var sw2      = Stopwatch.StartNew();
    var results2 = await runnerPar.RunAllAsync(testAssembly);
    sw2.Stop();

    Console.WriteLine($"  Время (параллельно Task.Run): {sw2.ElapsedMilliseconds} мс");

    Console.WriteLine();
    Console.WriteLine("*******************************************************");
    Console.WriteLine("     ДИНАМИЧЕСКИЙ ПУЛ ПОТОКОВ (Thread+Monitor)         ");
    Console.WriteLine("*******************************************************");

    var poolOptions = new ThreadPoolOptions
    {
        MinThreads        = 2,
        MaxThreads        = 4,
        IdleTimeoutMs     = 2000,
        ScaleUpWaitMs     = 300,
        MonitorIntervalMs = 150,
        HangThresholdMs   = 5000
    };

    using var pool = new DynamicThreadPool(poolOptions);

    int lastActive = -1;
    pool.StatsUpdated += stats =>
    {
        if (stats.ActiveThreads != lastActive)
        {
            lastActive = stats.ActiveThreads;
            Console.WriteLine($"   ПУЛМОНИТОР: {stats}");
        }
    };

    var optionsDyn = new TestRunnerOptions { MaxDegreeOfParallelism = 4, ParallelizeMethods = true };
    var runnerDyn  = new TestRunner(optionsDyn);

    var sw3      = Stopwatch.StartNew();
    var results3 = await runnerDyn.RunAllWithPoolAsync(testAssembly, pool.Enqueue);
    sw3.Stop();

    pool.Shutdown(5000);
    Console.WriteLine($"  Время (DynamicThreadPool):    {sw3.ElapsedMilliseconds} мс");
    Console.WriteLine();
    
    Console.WriteLine("*******************************************************");
    Console.WriteLine("                  СРАВНЕНИЕ ВРЕМЁН                    ");
    Console.WriteLine("*******************************************************");
    Console.WriteLine($"  Последовательно:      {sw1.ElapsedMilliseconds} мс");
    Console.WriteLine($"  Параллельно (Task):   {sw2.ElapsedMilliseconds} мс");
    Console.WriteLine($"  Динамический пул:     {sw3.ElapsedMilliseconds} мс");

    if (sw2.ElapsedMilliseconds < sw1.ElapsedMilliseconds)
    {
        double su2 = (double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds;
        Console.WriteLine($"  Ускорение Task.Run:   ×{su2:F2}");
    }
    if (sw3.ElapsedMilliseconds < sw1.ElapsedMilliseconds)
    {
        double su3 = (double)sw1.ElapsedMilliseconds / sw3.ElapsedMilliseconds;
        Console.WriteLine($"  Ускорение DynPool:    ×{su3:F2}");
    }

    int failed = results3.Sum(r => r.Failed + r.Errors);
    return failed == 0 ? 0 : 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Критическая ошибка тест-раннера: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 2;
}