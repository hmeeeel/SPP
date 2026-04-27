using CustomThreadPool.Events;

public class PoolEventLogger
    {
        private readonly string? _logFilePath;
        private readonly bool _logToConsole;
        private readonly object _lock = new();

        public PoolEventLogger(string? logFilePath = null, bool logToConsole = true)
        {
            _logFilePath = logFilePath;
            _logToConsole = logToConsole;
        }


        public void OnLifecycleEvent(object? sender, PoolLifecycleEventArgs e)
        {
            string logMessage = e.ToString();

            lock (_lock)
            {
                if (_logToConsole)
                {
                    Console.WriteLine($"[POOL_EVENT] {logMessage}");
                }

                if (_logFilePath is not null) System.IO.File.AppendAllText(_logFilePath, $"{logMessage}\n");
            }
        }


        public void OnCriticalEvent(object? sender, PoolLifecycleEventArgs e)
        {
            if (e.EventType == PoolEventType.ThreadHung ||
                e.EventType == PoolEventType.ThreadException ||
                e.EventType == PoolEventType.TaskFailed)
            {
                OnLifecycleEvent(sender, e);
            }
        }
    }