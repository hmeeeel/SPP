using CustomThreadPool;
using FrameworkTesting.Testing;
using System.Diagnostics;
using System.Reflection;

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