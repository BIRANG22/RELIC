using System;
using System.Collections.Generic;
using UnityEngine;

public class EventBus : Singleton<EventBus>
{
    private Dictionary<Type, Action<object>> eventTable = new();

    public void Initialize()
    {
        eventTable.Clear();
    }
    public void Subscribe<T>(Action<T> callback)
    {
        var type = typeof(T);

        if (!eventTable.ContainsKey(type))
            eventTable[type] = delegate { };

        eventTable[type] += (obj) => callback((T)obj);
    }

    public void Unsubscribe<T>(Action<T> callback)
    {
        var type = typeof(T);

        if (eventTable.ContainsKey(type))
            eventTable[type] -= (obj) => callback((T)obj);
    }

    public void Publish<T>(T eventData)
    {
        var type = typeof(T);

        if (eventTable.TryGetValue(type, out var action))
        {
            action?.Invoke(eventData);
        }
    }
}