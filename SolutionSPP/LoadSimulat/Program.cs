using CustomThreadPool;
using LoadSimulat;
using System.Diagnostics;

object consoleLock = new();
void Log(string text)
{
    lock (consoleLock)
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {text}");
}

var options = new ThreadPoolOptions
{
    MinThreads        = 2,
    MaxThreads        = 8,
    IdleTimeoutMs     = 3000,
    ScaleUpWaitMs     = 400,
    MonitorIntervalMs = 200,
    HangThresholdMs   = 5000
};

Console.WriteLine("*******************************************************");
Console.WriteLine("        ДИНАМИЧЕСКИЙ ПУЛ ПОТОКОВ  — LoadSimulator      ");
Console.WriteLine("*******************************************************");
Console.WriteLine($"  MinThreads={options.MinThreads}, MaxThreads={options.MaxThreads}");
Console.WriteLine($"  IdleTimeout={options.IdleTimeoutMs}мс, ScaleUpWait={options.ScaleUpWaitMs}мс");
Console.WriteLine($"  HangThreshold={options.HangThresholdMs}мс");
Console.WriteLine();

using var pool = new DynamicThreadPool(options);

int  lastActive = -1, lastQueue = -1;
pool.StatsUpdated += stats =>
{
    if (stats.ActiveThreads != lastActive || stats.QueueLength != lastQueue)
    {
        lastActive = stats.ActiveThreads;
        lastQueue  = stats.QueueLength;
        lock (consoleLock)
            Console.WriteLine($"    МОНИТОР: {stats}");
    }
};

await LoadSimulat.LoadScenario.RunAsync(pool, Log);

var finalStats = pool.GetStats();
Console.WriteLine();
Console.WriteLine("*******************************************************");
Console.WriteLine("                  ИТОГОВАЯ СТАТИСТИКА                   ");
Console.WriteLine("*******************************************************");
Console.WriteLine($"  Всего поставлено в очередь: {finalStats.TotalEnqueued}");
Console.WriteLine($"  Успешно выполнено:          {finalStats.TotalCompleted}");
Console.WriteLine($"  Ошибок:                     {finalStats.TotalFailed}");
Console.WriteLine($"  Активных потоков (финал):   {finalStats.ActiveThreads}");
Console.WriteLine();
Console.WriteLine("*******************************************************");
Console.WriteLine("       СРАВНЕНИЕ: ПОСЛЕДОВАТЕЛЬНО и ДИНАМИЧЕСКИЙ ПУЛ   ");
Console.WriteLine("*******************************************************");

const int benchTasks   = 20;
const int benchWorkMs  = 200;

Log($"  Запуск {benchTasks} задач последовательно...");
var swSeq = Stopwatch.StartNew();
for (int i = 0; i < benchTasks; i++)
    Thread.Sleep(benchWorkMs);
swSeq.Stop();

Log($"  Запуск {benchTasks} задач через DynamicThreadPool...");
using var benchPool = new DynamicThreadPool(new ThreadPoolOptions
{
    MinThreads = 2, MaxThreads = 8
});

var benchDone  = 0;
var benchLock  = new object();
var benchReady = new ManualResetEventSlim(false);

var swPar = Stopwatch.StartNew();
for (int i = 0; i < benchTasks; i++)
{
    benchPool.Enqueue(() =>
    {
        Thread.Sleep(benchWorkMs);
        int done = Interlocked.Increment(ref benchDone);
        if (done >= benchTasks)
        {
            lock (benchLock)
                benchReady.Set();
        }
    });
}
benchReady.Wait(30_000);
swPar.Stop();

Console.WriteLine();
Console.WriteLine($"  Последовательно:   {swSeq.ElapsedMilliseconds} мс");
Console.WriteLine($"  Динамический пул:  {swPar.ElapsedMilliseconds} мс");

if (swPar.ElapsedMilliseconds < swSeq.ElapsedMilliseconds)
{
    double speedup = (double)swSeq.ElapsedMilliseconds / swPar.ElapsedMilliseconds;
    Console.WriteLine($"  Ускорение:         ×{speedup:F2}");
    Console.WriteLine($"  Динамический пул быстрее в {speedup:F2} раза");
}
else
{
    Console.WriteLine("  (Ускорение незначительно на данной машине)");
}

benchPool.Shutdown();
Console.WriteLine("\n  Готово. Нажмите любую клавишу...");
Console.ReadKey(true);