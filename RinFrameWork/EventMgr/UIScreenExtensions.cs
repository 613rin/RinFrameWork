// UIScreenExtensions.cs - 只有需要事件功能时才使用
using System.Collections.Generic;
using UnityEngine;

public static class UIScreenExtensions
{
    // 获取ContextId
    public static int GetContextId(this UIScreen screen)
    {
        var ctx = screen.GetComponent<UIContext>();
        return ctx != null ? ctx.contextId : 0;
    }
    
    // 发布带作用域的事件
    public static void PublishEvent<T>(this UIScreen screen, T evt) where T : EventBus.IContextEvent
    {
        evt.contextId = screen.GetContextId();
        EventBus.Publish(evt);
    }
    
    // 设置单个标签
    public static void SetLabel(this UIScreen screen, string key, string text)
    {
        screen.PublishEvent(new SetLabelText { key = key, text = text });
    }
    
    // 设置多个标签
    public static void SetLabels(this UIScreen screen, params (string key, string text)[] labels)
    {
        var dict = new Dictionary<string, string>();
        foreach (var (key, text) in labels)
        {
            dict[key] = text;
        }
        screen.PublishEvent(new SetMultipleLabels { labelTexts = dict });
    }
}