
using CustomThreadPool;
namespace LoadSimulat
{
    // Фаза 1  ПРОГРЕВ:       единичные подачи    - пул на MinThreads
    // Фаза 2  ПИКОВАЯ:       50 задач разом      - пул расширяется до MaxThreads
    // Фаза 3  БЕЗДЕЙСТВИЕ:   пауза 8 с           - пул сжимается до MinThreads
    // Фаза 4  ЕДИНИЧНЫЕ:     1 задача каждые 2 с - пул стабилен на MinThreads
    public static class LoadScenario
    {
        private static readonly object _consoleLock = new();
        private static int _taskCounter;

        public static async Task RunAsync(DynamicThreadPool pool, Action<string> log)
        {
            log("**********************************************************");
            log("  СЦЕНАРИЙ НАГРУЗКИ: старт");
            log("**********************************************************");

            // ФАЗА 1: Прогрев - 5 задач с паузами
            log("\n[Фаза 1] ПРОГРЕВ: 5 задач по одной (интервал 600 мс)");
            for (int i = 0; i < 5; i++)
            {
                EnqueueTask(pool, workMs: 300, log);
                await Task.Delay(600);
            }

            log($"  * после фазы 1: {pool.GetStats()}");
            await Task.Delay(1000);

            // ФАЗА 2: Пиковая нагрузка — 50 задач без паузы
            log("\n[Фаза 2] ПИКОВАЯ НАГРУЗКА: 50 задач подряд");
            for (int i = 0; i < 50; i++)
                EnqueueTask(pool, workMs: 400, log);

            log($"  * сразу после подачи: {pool.GetStats()}");

            // Ждём, пока большинство задач выполнятся
            await WaitUntilQueueNearEmpty(pool, log, timeoutMs: 30_000);
            log($"  * после завершения пиковых задач: {pool.GetStats()}");

            // ФАЗА 3: Бездействие - ждём сжатия пула
            log("\n[Фаза 3] БЕЗДЕЙСТВИЕ: пауза 8 секунд (ждём сжатия)");
            for (int s = 8; s > 0; s--)
            {
                log($"  ... ожидание {s}с — {pool.GetStats()}");
                await Task.Delay(1000);
            }
            log($"  * после бездействия: {pool.GetStats()}");

            // ФАЗА 4: Единичные подачи
            log("\n[Фаза 4] ЕДИНИЧНЫЕ ПОДАЧИ: 5 задач (интервал 2 с)");
            for (int i = 0; i < 5; i++)
            {
                EnqueueTask(pool, workMs: 200, log);
                log($"  * {pool.GetStats()}");
                await Task.Delay(2000);
            }

            await WaitUntilQueueNearEmpty(pool, log, timeoutMs: 10_000);

            log("\n**********************************************************");
            log("  СЦЕНАРИЙ НАГРУЗКИ: завершён");
            log($"  Итог: {pool.GetStats()}");
            log("**********************************************************\n");
        }

        private static void EnqueueTask(DynamicThreadPool pool, int workMs, Action<string> log)
        {
            int num = Interlocked.Increment(ref _taskCounter);
            string name = $"Task-{num:D3}";

            pool.Enqueue(() =>
            {
                int tid = Thread.CurrentThread.ManagedThreadId;
                lock (_consoleLock)
                    log($"    -> {name} START  [T{tid:D2}]");

                SimulateWork(workMs);

                lock (_consoleLock)
                    log($"    <- {name} FINISH [T{tid:D2}]");

            }, name);
        }

        private static void SimulateWork(int durationMs)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Thread.Sleep(durationMs);
            sw.Stop();
        }

        private static async Task WaitUntilQueueNearEmpty(
            DynamicThreadPool pool, Action<string> log, int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                var stats = pool.GetStats();
                if (stats.QueueLength == 0 && stats.BusyThreads == 0) break;
                await Task.Delay(500);
            }
        }
    }
}