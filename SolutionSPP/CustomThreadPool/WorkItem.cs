namespace CustomThreadPool
{
    internal sealed class WorkItem
    {
        public Action    Task       { get; }
        public DateTime  EnqueuedAt { get; }
        public string    Name       { get; }

        public WorkItem(Action task, string name = "")
        {
            Task       = task;
            EnqueuedAt = DateTime.UtcNow;
            Name       = name;
        }

        // то колько миллисекунд задача провела в очереди
        public double WaitMs => (DateTime.UtcNow - EnqueuedAt).TotalMilliseconds;
    }
}