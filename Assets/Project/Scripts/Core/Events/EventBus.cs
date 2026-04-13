using System;
using System.Collections.Generic;

public class EventBus : Singleton<EventBus>
{
    private readonly Dictionary<Type, Delegate> eventTable = new();

    public void Initialize()
    {
        eventTable.Clear();
    }

    public void Subscribe<T>(Action<T> callback)
    {
        if (callback == null)
        {
            return;
        }

        var type = typeof(T);

        if (eventTable.TryGetValue(type, out var existingHandler))
        {
            eventTable[type] = Delegate.Combine(existingHandler, callback);
            return;
        }

        eventTable[type] = callback;
    }

    public void Unsubscribe<T>(Action<T> callback)
    {
        if (callback == null)
        {
            return;
        }

        var type = typeof(T);

        if (!eventTable.TryGetValue(type, out var existingHandler))
        {
            return;
        }

        var newHandler = Delegate.Remove(existingHandler, callback);
        if (newHandler == null)
        {
            eventTable.Remove(type);
            return;
        }

        eventTable[type] = newHandler;
    }

    public void Publish<T>(T eventData)
    {
        var type = typeof(T);

        if (eventTable.TryGetValue(type, out var handler) && handler is Action<T> action)
        {
            action.Invoke(eventData);
        }
    }
}
