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
    var runnerSeq = new TestRunner(optionsSeq);
 
    var sw1 = Stopwatch.StartNew();
    var results1 = await runnerSeq.RunAllAsync(testAssembly);
    sw1.Stop();
 
    Console.WriteLine($"  Время (последовательно): {sw1.ElapsedMilliseconds} мс");
 
    Console.WriteLine();
    Console.WriteLine("*******************************************************");
    Console.WriteLine("                 ПАРАЛЛЕЛЬНЫЙ ЗАПУСК 2                 ");
    Console.WriteLine("*******************************************************");
 
    var optionsPar = new TestRunnerOptions { MaxDegreeOfParallelism = 4, ParallelizeMethods = true };
    var runnerPar = new TestRunner(optionsPar);
 
    var sw2 = Stopwatch.StartNew();
    var results2 = await runnerPar.RunAllAsync(testAssembly);
    sw2.Stop();
 
    Console.WriteLine($"  Время (параллельно):     {sw2.ElapsedMilliseconds} мс");
 
    Console.WriteLine();
    Console.WriteLine($"  Последовательно: {sw1.ElapsedMilliseconds} мс");
    Console.WriteLine($"  Параллельно:     {sw2.ElapsedMilliseconds} мс");
 
    if (sw2.ElapsedMilliseconds < sw1.ElapsedMilliseconds)
    {
        double speedup = (double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds;
        Console.WriteLine($"  Ускорение:       x{speedup:F2}");
    }
    else
    {
        Console.WriteLine("  Без ускорения");
    }
 
    int failed = results2.Sum(r => r.Failed + r.Errors);
    return failed == 0 ? 0 : 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Критическая ошибка тест-раннера: {ex.Message}");
    return 2;
}