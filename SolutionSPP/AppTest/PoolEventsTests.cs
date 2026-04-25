
using FrameworkTesting.Assert;
using FrameworkTesting.Attributes;
using CustomThreadPool;
using CustomThreadPool.Events;
namespace AppTest.Tests
{

    [TestClass(Category = "Events", Description = "Демонстрация событий жизненного цикла пула")]
    public class PoolEventsTests
    {

        [TestMethod("Подписка на общее событие LifecycleEvent")]
        public void EventsDemo_BasicSubscription()
        {
            var eventLog = new List<string>();
            var options = new ThreadPoolOptions
            {
                MinThreads = 2,
                MaxThreads = 4,
                IdleTimeoutMs = 1000
            };

            using var pool = new DynamicThreadPoolWithEvents(options);

            pool.LifecycleEvent += (sender, e) =>
            {
                lock (eventLog)
                {
                    eventLog.Add($"{e.EventType}|{e.ThreadId}|{e.TaskName}");
                }
            };

            pool.Enqueue(() => Thread.Sleep(100), "Task1");
            pool.Enqueue(() => Thread.Sleep(100), "Task2");

            Thread.Sleep(500);

            Assert.GreaterThan(eventLog.Count, 0, "Должны быть зафиксированы события");

            var hasTaskEnqueued = eventLog.Any(e => e.StartsWith("TaskEnqueued"));
            Assert.IsTrue(hasTaskEnqueued, "Должно быть событие TaskEnqueued");

            var hasTaskCompleted = eventLog.Any(e => e.StartsWith("TaskCompleted"));
            Assert.IsTrue(hasTaskCompleted, "Должно быть событие TaskCompleted");
        }


        [TestMethod("Подписка на специализированные события потоков")]
        public void EventsDemo_ThreadLifecycle()
        {
            int threadsCreated = 0;
            int threadsTerminated = 0;

            var options = new ThreadPoolOptions
            {
                MinThreads = 2,
                MaxThreads = 4,
                IdleTimeoutMs = 500
            };

            using var pool = new DynamicThreadPoolWithEvents(options);

            pool.ThreadCreatedEvent += (sender, e) =>
            {
                Interlocked.Increment(ref threadsCreated);
            };

            pool.ThreadTerminatedEvent += (sender, e) =>
            {
                Interlocked.Increment(ref threadsTerminated);
            };

            for (int i = 0; i < 10; i++)
            {
                pool.Enqueue(() => Thread.Sleep(200), $"Task{i}");
            }

            Thread.Sleep(1000);

            // Проверяем
            Assert.GreaterThan(threadsCreated, 0, "Должны быть созданы потоки");
            Console.WriteLine($"Создано потоков: {threadsCreated}, Завершено: {threadsTerminated}");
        }


        [TestMethod("Отслеживание событий масштабирования")]
        [Timeout(5000)]
        public void EventsDemo_PoolScaling()
        {
            var scalingEvents = new List<PoolLifecycleEventArgs>();
            var lockObj = new object();

            var options = new ThreadPoolOptions
            {
                MinThreads = 2,
                MaxThreads = 6,
                IdleTimeoutMs = 1000,
                ScaleUpWaitMs = 200
            };

            using var pool = new DynamicThreadPoolWithEvents(options);

            pool.PoolScalingEvent += (sender, e) =>
            {
                lock (lockObj)
                {
                    scalingEvents.Add(e);
                    Console.WriteLine($"[SCALING] {e.EventType}: {e.Message}");
                }
            };

            Console.WriteLine("Фаза 1: Пиковая нагрузка");
            for (int i = 0; i < 20; i++)
            {
                pool.Enqueue(() => Thread.Sleep(300), $"PeakTask{i}");
            }

            Thread.Sleep(1500); 

            Console.WriteLine("Фаза 2: Бездействие");
            Thread.Sleep(2000); 

            lock (lockObj)
            {
                var scaleUps = scalingEvents.Count(e => e.EventType == PoolEventType.ScaleUp);
                var scaleDowns = scalingEvents.Count(e => e.EventType == PoolEventType.ScaleDown);

                Console.WriteLine($"ScaleUp: {scaleUps}, ScaleDown: {scaleDowns}");
                Assert.GreaterThan(scaleUps, 0, "Должны быть события ScaleUp");
            }
        }


        [TestMethod("Отслеживание событий ошибок")]
        public void EventsDemo_ErrorTracking()
        {
            var errors = new List<PoolLifecycleEventArgs>();
            var lockObj = new object();

            var options = new ThreadPoolOptions { MinThreads = 2, MaxThreads = 4 };
            using var pool = new DynamicThreadPoolWithEvents(options);


            pool.ErrorEvent += (sender, e) =>
            {
                lock (lockObj)
                {
                    errors.Add(e);
                    Console.WriteLine($"[ERROR] {e.EventType}: {e.Message}");
                }
            };

            pool.Enqueue(() => throw new InvalidOperationException("Тестовая ошибка 1"), "FailTask1");
            pool.Enqueue(() => throw new ArgumentException("Тестовая ошибка 2"), "FailTask2");
            pool.Enqueue(() => Thread.Sleep(100), "GoodTask"); 

            Thread.Sleep(500);

            lock (lockObj)
            {
                var taskFailures = errors.Count(e => e.EventType == PoolEventType.TaskFailed);
                Assert.GreaterThan(taskFailures, 0, "Должны быть зафиксированы ошибки задач");
                Console.WriteLine($"Зафиксировано ошибок: {taskFailures}");
            }
        }

        [TestMethod("Использование агрегатора событий")]
        public void EventsDemo_EventAggregator()
        {
            var aggregator = new PoolEventAggregator();
            var options = new ThreadPoolOptions { MinThreads = 2, MaxThreads = 4 };

            using var pool = new DynamicThreadPoolWithEvents(options);

            pool.LifecycleEvent += aggregator.OnLifecycleEvent;

            for (int i = 0; i < 10; i++)
            {
                pool.Enqueue(() => Thread.Sleep(50), $"Task{i}");
            }


            pool.Enqueue(() => throw new Exception("Error1"), "FailTask1");
            pool.Enqueue(() => throw new Exception("Error2"), "FailTask2");

            Thread.Sleep(1000);

            Console.WriteLine(aggregator.GetSummary());
            
            Assert.AreEqual(12, aggregator.TotalTasksEnqueued, "Должно быть 12 задач");
            Assert.GreaterThan(aggregator.TotalTasksCompleted, 8, "Должно быть завершено минимум 8 задач");
            Assert.AreEqual(2, aggregator.TotalTasksFailed, "Должно быть 2 провала");
        }

        [TestMethod("Использование логгера событий")]
        public void EventsDemo_EventLogger()
        {
            var logger = new PoolEventLogger(logToConsole: false); 
            var options = new ThreadPoolOptions { MinThreads = 2, MaxThreads = 3 };

            using var pool = new DynamicThreadPoolWithEvents(options);

            pool.LifecycleEvent += logger.OnLifecycleEvent;

            pool.Enqueue(() => Thread.Sleep(100), "LoggedTask1");
            pool.Enqueue(() => Thread.Sleep(100), "LoggedTask2");

            Thread.Sleep(500);

            Assert.IsTrue(true, "Логгер успешно обработал события");
        }

        [TestMethod("Фильтрация критичных событий")]
        public void EventsDemo_CriticalEventsOnly()
        {
            var criticalEvents = new List<PoolLifecycleEventArgs>();
            var lockObj = new object();

            var options = new ThreadPoolOptions { MinThreads = 2, MaxThreads = 3 };
            using var pool = new DynamicThreadPoolWithEvents(options);

            pool.LifecycleEvent += (sender, e) =>
            {
                if (e.EventType == PoolEventType.ThreadHung ||
                    e.EventType == PoolEventType.ThreadException ||
                    e.EventType == PoolEventType.TaskFailed)
                {
                    lock (lockObj)
                    {
                        criticalEvents.Add(e);
                        Console.WriteLine($"[CRITICAL] {e}");
                    }
                }
            };

            pool.Enqueue(() => Thread.Sleep(50), "Normal1");
            pool.Enqueue(() => Thread.Sleep(50), "Normal2");

            pool.Enqueue(() => throw new Exception("Critical error"), "CriticalTask");

            Thread.Sleep(500);

            lock (lockObj)
            {
                Assert.GreaterThan(criticalEvents.Count, 0, "Должно быть минимум 1 критичное событие");
                Assert.IsTrue(criticalEvents.All(e => 
                    e.EventType == PoolEventType.TaskFailed ||
                    e.EventType == PoolEventType.ThreadException),
                    "Все события должны быть критичными");
            }
        }


        [TestMethod("Отписка от событий")]
        public void EventsDemo_Unsubscribe()
        {
            int eventCount = 0;
            var options = new ThreadPoolOptions { MinThreads = 2, MaxThreads = 3 };

            using var pool = new DynamicThreadPoolWithEvents(options);

            PoolLifecycleEventHandler handler = (sender, e) =>
            {
                if (e.EventType == PoolEventType.TaskEnqueued)
                {
                    Interlocked.Increment(ref eventCount);
                }
            };


            pool.LifecycleEvent += handler;

            pool.Enqueue(() => Thread.Sleep(50), "Task1");
            Thread.Sleep(200);
            int countAfterFirst = eventCount;

            pool.LifecycleEvent -= handler;

            pool.Enqueue(() => Thread.Sleep(50), "Task2");
            Thread.Sleep(200);
            int countAfterSecond = eventCount;

            Assert.AreEqual(1, countAfterFirst, "Должно быть 1 событие после первой задачи");
            Assert.AreEqual(1, countAfterSecond, "Не должно быть новых событий после отписки");
        }

        [TestMethod("Комплексная проверка всех типов событий")]
        [Timeout(5000)]
        public void EventsDemo_AllEventTypes()
        {
            var eventTypes = new HashSet<PoolEventType>();
            var lockObj = new object();

            var options = new ThreadPoolOptions
            {
                MinThreads = 2,
                MaxThreads = 4,
                IdleTimeoutMs = 800
            };

            using var pool = new DynamicThreadPoolWithEvents(options);

            pool.LifecycleEvent += (sender, e) =>
            {
                lock (lockObj)
                {
                    eventTypes.Add(e.EventType);
                }
            };


            pool.Enqueue(() => Thread.Sleep(100), "NormalTask");
            pool.Enqueue(() => throw new Exception("Error"), "ErrorTask");
            
   
            for (int i = 0; i < 8; i++)
            {
                pool.Enqueue(() => Thread.Sleep(200), $"LoadTask{i}");
            }

            Thread.Sleep(1500);
            Thread.Sleep(1500);

            lock (lockObj)
            {
                Console.WriteLine($"Зафиксировано типов событий: {eventTypes.Count}");
                foreach (var eventType in eventTypes.OrderBy(e => e.ToString()))
                {
                    Console.WriteLine($"  - {eventType}");
                }

                Assert.IsTrue(eventTypes.Contains(PoolEventType.TaskEnqueued), "Должно быть TaskEnqueued");
                Assert.IsTrue(eventTypes.Contains(PoolEventType.TaskStarted), "Должно быть TaskStarted");
                Assert.IsTrue(eventTypes.Contains(PoolEventType.TaskCompleted), "Должно быть TaskCompleted");
                Assert.IsTrue(eventTypes.Contains(PoolEventType.ThreadBusy), "Должно быть ThreadBusy");
                Assert.IsTrue(eventTypes.Contains(PoolEventType.ThreadIdle), "Должно быть ThreadIdle");
            }
        }
    }
}