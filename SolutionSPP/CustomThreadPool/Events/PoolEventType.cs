using System;

namespace CustomThreadPool.Events
{
    public enum PoolEventType
    {
        PoolCreated,           // Пул создан
        ThreadCreated,         // Поток создан
        ThreadStarted,         // Поток запущен
        ThreadIdle,            // Поток простаивает
        ThreadBusy,            // Поток занят задачей
        ThreadTerminated,      // Поток завершён
        ThreadHung,            // Поток завис
        ThreadReplaced,        // Поток заменён после зависания
        ThreadException,       // Исключение в потоке
        TaskEnqueued,          // Задача поставлена в очередь
        TaskDequeued,          // Задача извлечена из очереди
        TaskStarted,           // Задача начала выполнение
        TaskCompleted,         // Задача завершена успешно
        TaskFailed,            // Задача завершена с ошибкой
        ScaleUp,               // Пул расширяется (увеличение потоков)
        ScaleDown,             // Пул сжимается (уменьшение потоков)
        QueueFull,             // Очередь заполнена
        QueueEmpty,            // Очередь пуста
        PoolShuttingDown,      // Пул начинает завершение работы
        PoolShutdown           // Пул полностью остановлен
    }

    public class PoolLifecycleEventArgs : EventArgs
    {
        public PoolEventType EventType { get; }
        public DateTime Timestamp { get; }
        public int? ThreadId { get; }
        public string? TaskName { get; }
        public Exception? Exception { get; }
        public string? Message { get; }
        public ThreadPoolStats? Stats { get; }

        public PoolLifecycleEventArgs(
            PoolEventType eventType,
            int? threadId = null,
            string? taskName = null,
            Exception? exception = null,
            string? message = null,
            ThreadPoolStats? stats = null)
        {
            EventType = eventType;
            Timestamp = DateTime.UtcNow;
            ThreadId = threadId;
            TaskName = taskName;
            Exception = exception;
            Message = message;
            Stats = stats;
        }

        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string>
            {
                $"[{Timestamp:HH:mm:ss.fff}]",
                $"{EventType}"
            };

            if (ThreadId.HasValue)
                parts.Add($"T{ThreadId.Value:D2}");

            if (!string.IsNullOrEmpty(TaskName))
                parts.Add($"Task:{TaskName}");

            if (!string.IsNullOrEmpty(Message))
                parts.Add(Message);

            if (Exception is not null)
                parts.Add($"Error:{Exception.GetType().Name}");

            return string.Join(" | ", parts);
        }
    }

    public delegate void PoolLifecycleEventHandler(
        object sender, 
        PoolLifecycleEventArgs e);

    public interface IPoolLifecycleEvents
    {
        event PoolLifecycleEventHandler? LifecycleEvent;
        event PoolLifecycleEventHandler? ThreadCreatedEvent;
        event PoolLifecycleEventHandler? ThreadTerminatedEvent;
        event PoolLifecycleEventHandler? PoolScalingEvent;
        event PoolLifecycleEventHandler? ErrorEvent;
    }


    public class PoolEventAggregator
    {
        private int _totalTasksEnqueued;
        private int _totalTasksCompleted;
        private int _totalTasksFailed;
        private int _totalThreadsCreated;
        private int _totalThreadsTerminated;
        private int _totalScaleUps;
        private int _totalScaleDowns;
        private int _totalHungThreads;
        
        private readonly object _lock = new();

        public int TotalTasksEnqueued => _totalTasksEnqueued;
        public int TotalTasksCompleted => _totalTasksCompleted;
        public int TotalTasksFailed => _totalTasksFailed;
        public int TotalThreadsCreated => _totalThreadsCreated;
        public int TotalThreadsTerminated => _totalThreadsTerminated;
        public int TotalScaleUps => _totalScaleUps;
        public int TotalScaleDowns => _totalScaleDowns;
        public int TotalHungThreads => _totalHungThreads;


        public void OnLifecycleEvent(object? sender, PoolLifecycleEventArgs e)
        {
            lock (_lock)
            {
                switch (e.EventType)
                {
                    case PoolEventType.TaskEnqueued:
                        System.Threading.Interlocked.Increment(ref _totalTasksEnqueued);
                        break;

                    case PoolEventType.TaskCompleted:
                        System.Threading.Interlocked.Increment(ref _totalTasksCompleted);
                        break;

                    case PoolEventType.TaskFailed:
                        System.Threading.Interlocked.Increment(ref _totalTasksFailed);
                        break;

                    case PoolEventType.ThreadCreated:
                        System.Threading.Interlocked.Increment(ref _totalThreadsCreated);
                        break;

                    case PoolEventType.ThreadTerminated:
                        System.Threading.Interlocked.Increment(ref _totalThreadsTerminated);
                        break;

                    case PoolEventType.ScaleUp:
                        System.Threading.Interlocked.Increment(ref _totalScaleUps);
                        break;

                    case PoolEventType.ScaleDown:
                        System.Threading.Interlocked.Increment(ref _totalScaleDowns);
                        break;

                    case PoolEventType.ThreadHung:
                        System.Threading.Interlocked.Increment(ref _totalHungThreads);
                        break;
                }
            }
        }

        public string GetSummary()
        {
            lock (_lock)
            {
                return $"EventAggregator: " +
                       $"Tasks(E:{_totalTasksEnqueued}/C:{_totalTasksCompleted}/F:{_totalTasksFailed}) | " +
                       $"Threads(Created:{_totalThreadsCreated}/Terminated:{_totalThreadsTerminated}/Hung:{_totalHungThreads}) | " +
                       $"Scaling(Up:{_totalScaleUps}/Down:{_totalScaleDowns})";
            }
        }
    }
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
}