using CustomThreadPool.Events;

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