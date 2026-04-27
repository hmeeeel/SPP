using CustomThreadPool.Events;

public interface IPoolLifecycleEvents
    {
        event PoolLifecycleEventHandler? LifecycleEvent;
        event PoolLifecycleEventHandler? ThreadCreatedEvent;
        event PoolLifecycleEventHandler? ThreadTerminatedEvent;
        event PoolLifecycleEventHandler? PoolScalingEvent;
        event PoolLifecycleEventHandler? ErrorEvent;
    }