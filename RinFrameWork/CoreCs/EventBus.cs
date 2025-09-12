using System;
using System.Collections.Generic;

public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> _table = new();

    
    // 添加一个支持作用域过滤的发布方法
    public static void PublishScoped<T>(T evt, int contextId) where T : IContextEvent
    {
        evt.contextId = contextId;
        Publish(evt);
    }
    
    // 添加一个接口标记需要作用域的事件
    public interface IContextEvent
    {
        int contextId { get; set; }
    }
    public static void Subscribe<T>(Action<T> handler)
    {
        if (handler == null) return;
        var t = typeof(T);
        if (_table.TryGetValue(t, out var del))
            _table[t] = (Action<T>)del + handler;
        else
            _table[t] = handler;
    }

    public static void Unsubscribe<T>(Action<T> handler)
    {
        if (handler == null) return;
        var t = typeof(T);
        if (_table.TryGetValue(t, out var del))
        {
            var cur = (Action<T>)del - handler;
            if (cur == null) _table.Remove(t);
            else _table[t] = cur;
        }
    }

    public static void Publish<T>(T evt)
    {
        if (_table.TryGetValue(typeof(T), out var del))
            ((Action<T>)del)?.Invoke(evt);
    }

    // 可选：切场景或整体重置时调用
    public static void Clear() => _table.Clear();
}