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

   

    public delegate void PoolLifecycleEventHandler(
        object sender, 
        PoolLifecycleEventArgs e);

}