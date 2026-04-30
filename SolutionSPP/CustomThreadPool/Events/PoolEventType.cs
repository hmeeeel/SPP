using System;

namespace CustomThreadPool.Events
{
    public enum PoolEventType
    {
        PoolCreated, ThreadCreated, ThreadStarted, ThreadIdle, ThreadBusy, 
        ThreadHung, ThreadReplaced, ThreadException, TaskEnqueued, 
        TaskDequeued, TaskStarted, TaskCompleted, TaskFailed, ScaleUp,         
        ScaleDown, QueueFull, QueueEmpty, PoolShuttingDown, PoolShutdown, ThreadTerminated
    }

    public delegate void PoolLifecycleEventHandler(
        object sender, 
        PoolLifecycleEventArgs e);

}