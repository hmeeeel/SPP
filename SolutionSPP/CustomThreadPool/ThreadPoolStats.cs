namespace CustomThreadPool
{
    public sealed class ThreadPoolStats
    {
        public int      ActiveThreads  { get; init; }
        public int      IdleThreads    { get; init; }
        public int      BusyThreads    => ActiveThreads - IdleThreads;
        public int      QueueLength    { get; init; }
        public long     TotalEnqueued  { get; init; }
        public long     TotalCompleted { get; init; }
        public long     TotalFailed    { get; init; }
        public DateTime Timestamp      { get; init; }

        public override string ToString() =>
            $"[{Timestamp:HH:mm:ss.fff}] " +
            $"Потоки: {ActiveThreads} (занято={BusyThreads}, idle={IdleThreads}) | " +
            $"Очередь: {QueueLength} | " +
            $"Выполнено: {TotalCompleted}/{TotalEnqueued} | " +
            $"Ошибок: {TotalFailed}";
    }
}