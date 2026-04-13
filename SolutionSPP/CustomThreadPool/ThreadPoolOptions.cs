namespace CustomThreadPool
{
    public class ThreadPoolOptions
    {
        private int _minThreads = 2;
        private int _maxThreads = 8;

        public int MinThreads
        {
            get => _minThreads;
            set => _minThreads = Math.Max(1, value);
        }

        public int MaxThreads
        {
            get => _maxThreads;
            set => _maxThreads = Math.Max(_minThreads, value);
        }

        public int IdleTimeoutMs { get; set; } = 3000; // активных потоков > MinThreads
        public int ScaleUpWaitMs { get; set; } = 400; //  > иниц нов поток
        public int MonitorIntervalMs { get; set; } = 200; //интервал проверки монитора
        public int HangThresholdMs { get; set; } = 5000; // > - завис и замена
    }
}