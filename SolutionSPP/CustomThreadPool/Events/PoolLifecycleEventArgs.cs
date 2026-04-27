using CustomThreadPool;
using CustomThreadPool.Events;

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