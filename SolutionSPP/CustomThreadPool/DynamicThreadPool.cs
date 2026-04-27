namespace CustomThreadPool
{
    public sealed class DynamicThreadPool : IDisposable
    {
        private readonly Queue<WorkItem> _queue = new();
        private readonly object _queueLock = new();

        private readonly ThreadPoolOptions _options;

        private int  _activeThreads, _idleThreads;
        private long _totalEnqueued, _totalCompleted, _totalFailed; 
        private readonly Dictionary<int, DateTime> _heartbeat = new();
        private readonly Dictionary<int, CancellationTokenSource> _threadCts = new();
        private readonly object _heartbeatLock = new();

        private volatile bool _shutdown;
        private readonly Thread _monitorThread;

        private readonly object _consoleLock = new();
        public event Action<ThreadPoolStats>? StatsUpdated;
        private readonly List<Thread> _workerThreads = new();
        private readonly object _threadListLock = new();
        private readonly Dictionary<int, bool>     _threadBusy  = new(); // true = выполняет задачу

        private readonly object _monitorSignal = new();
        public DynamicThreadPool(ThreadPoolOptions? options = null)
        {
            _options = options ?? new ThreadPoolOptions();

            for (int i = 0; i < _options.MinThreads; i++)
                AddWorkerThread();

            _monitorThread = new Thread(MonitorLoop)
            {
                IsBackground = true,
                Name         = "DynPool-Monitor"
            };
            _monitorThread.Start();
        }

        public void Enqueue(Action task, string name = "")
        {
            if (_shutdown)
                throw new InvalidOperationException("Пул завершает работу - постановка задач невозможна.");

            var item = new WorkItem(task, name);

            lock (_queueLock)
            {
                _queue.Enqueue(item);
                Interlocked.Increment(ref _totalEnqueued);
                Monitor.Pulse(_queueLock);
            }
        }

        // Текущий снимок состояния пула
        public ThreadPoolStats GetStats() => new()
        {
            ActiveThreads  = _activeThreads,
            IdleThreads    = _idleThreads,
            QueueLength    = GetQueueLength(),
            TotalEnqueued  = Interlocked.Read(ref _totalEnqueued),
            TotalCompleted = Interlocked.Read(ref _totalCompleted),
            TotalFailed    = Interlocked.Read(ref _totalFailed),
            Timestamp      = DateTime.UtcNow
        };

        public void Shutdown(int waitMs = 15_000)
        {
            _shutdown = true;
 
            lock (_queueLock) Monitor.PulseAll(_queueLock);
 
            lock (_monitorSignal) Monitor.Pulse(_monitorSignal);
 
            List<Thread> threads;
            lock (_threadListLock)
                threads = new List<Thread>(_workerThreads);
 
            var deadline = DateTime.UtcNow.AddMilliseconds(waitMs);
            foreach (var t in threads)
            {
                int remaining = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                if (remaining <= 0) break;
                t.Join(remaining);
            }

            int monRemaining = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
            if (monRemaining > 0)
                _monitorThread.Join(monRemaining);
        }
 
        public void Dispose() => Shutdown();

        private void AddWorkerThread(string? reason = null)
        {
            int newCount = Interlocked.Increment(ref _activeThreads);
            if (newCount > _options.MaxThreads)
            {
                Interlocked.Decrement(ref _activeThreads);
                return;
            }

            var cts    = new CancellationTokenSource();
            var thread = new Thread(() => WorkerLoop(cts.Token))
            {
                IsBackground = true,
                Name         = $"DynPool-Worker-{Environment.CurrentManagedThreadId}"
            };
 
            // Регистрируем поток 
            lock (_threadListLock)
                _workerThreads.Add(thread);
 
            lock (_heartbeatLock)
            {
                _heartbeat[thread.ManagedThreadId]  = DateTime.UtcNow;
                _threadCts[thread.ManagedThreadId]  = cts;
                _threadBusy[thread.ManagedThreadId] = false;
            }
 
            if (reason is not null)
                SafeLog($"[POOL] +Поток (причина: {reason}). Активно: {_activeThreads}");

            thread.Start();
        }

        private void WorkerLoop(CancellationToken cancelToken)
        {
            int tid = Thread.CurrentThread.ManagedThreadId;

            while (!_shutdown && !cancelToken.IsCancellationRequested)
            {
                WorkItem? item = null;
                bool shouldExit = false;

                lock (_queueLock)
                {
                    Interlocked.Increment(ref _idleThreads);

                    while (_queue.Count == 0 && !_shutdown && !cancelToken.IsCancellationRequested)
                    {
                        bool signaled = Monitor.Wait(_queueLock, _options.IdleTimeoutMs);

                        if (!signaled && _activeThreads > _options.MinThreads)
                        {
                            Interlocked.Decrement(ref _idleThreads);
                            shouldExit = true;
                            break;
                        }
                    }

                    if (!shouldExit)
                    {
                        Interlocked.Decrement(ref _idleThreads);

                        if (_queue.Count > 0)
                            item = _queue.Dequeue();
                    }
                }

                if (shouldExit)
                    break;

                if (item is null)
                    continue;

                // поток начал выполнение задачи
                lock (_heartbeatLock)
                    _heartbeat[tid] = DateTime.UtcNow;

                ExecuteItem(item, tid);

                // сброс - поток  свободен
                lock (_heartbeatLock)
                    _heartbeat[tid] = DateTime.UtcNow;
            }

            lock (_heartbeatLock)
            {
                _heartbeat.Remove(tid);
                if (_threadCts.TryGetValue(tid, out var cts))
                {
                    _threadCts.Remove(tid);
                    cts.Dispose();
                }
            }

            int remaining = Interlocked.Decrement(ref _activeThreads);
            SafeLog($"[POOL] -Поток {tid} завершён. Активно: {remaining}");
        }

        private void ExecuteItem(WorkItem item, int tid)
        {
            try
            {
                item.Task();
                Interlocked.Increment(ref _totalCompleted);
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref _totalCompleted);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalFailed);
                SafeLog($"[ERROR] Задача '{item.Name}' (поток {tid}): {ex.GetType().Name}: {ex.Message}");

                if (_activeThreads < _options.MinThreads && !_shutdown)
                    AddWorkerThread("восстановление после сбоя");
            }
        }

        private void MonitorLoop()
        {
            while (!_shutdown)
            {
                Thread.Sleep(_options.MonitorIntervalMs);
                if (_shutdown) break;

                int qLen = GetQueueLength();

                //1
                if (qLen > 0 && _idleThreads == 0 && _activeThreads < _options.MaxThreads)
                {
                    AddWorkerThread($"очередь={qLen}, idle=0");
                }

                //2
                CheckLongWaitingTasks();

                ReplaceHungThreads(qLen);

                var stats = GetStats();
                StatsUpdated?.Invoke(stats);
            }
        }

        private void CheckLongWaitingTasks()
        {
            WorkItem? oldest = null;
            lock (_queueLock)
            {
                if (_queue.Count > 0)
                    oldest = _queue.Peek();
            }

            // проверка ст зад если дольше ScaleUpWaitMs и лимит не достигнут
            if (oldest is not null
                && oldest.WaitMs > _options.ScaleUpWaitMs
                && _activeThreads < _options.MaxThreads)
            {
                AddWorkerThread($"задача '{oldest.Name}' ждёт {oldest.WaitMs:F0}мс");
            }
        }

        // Поток считается зависшим, если не обновлял heartbeat дольше HangThresholdMs при непустой очереди
        private void ReplaceHungThreads(int queueLen)
        {
            if (queueLen == 0) return;

            List<(int tid, CancellationTokenSource cts)> hung = [];

            lock (_heartbeatLock)
            {
                var now = DateTime.UtcNow;
                foreach (var (tid, lastSeen) in _heartbeat)
                {
                    if ((now - lastSeen).TotalMilliseconds > _options.HangThresholdMs
                        && _threadCts.TryGetValue(tid, out var cts))
                    {
                        hung.Add((tid, cts));
                    }
                }
            }

            foreach (var (tid, cts) in hung)
            {
                SafeLog($"[POOL] Поток {tid} завис — отменяем и заменяем");
                cts.Cancel();
                AddWorkerThread($"замена зависшего {tid}");
            }
        }

        private int GetQueueLength()
        {
            lock (_queueLock) return _queue.Count;
        }
        
        private void SafeLog(string text)
        {
            lock (_consoleLock)
                Console.WriteLine($"[T{Thread.CurrentThread.ManagedThreadId:D2}] {text}");
        }
    }
}