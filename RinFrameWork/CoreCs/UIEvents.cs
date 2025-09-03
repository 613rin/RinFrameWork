using System;

/*
Copyright (c) 2025 LiYun. All rights reserved.

本框架（包括但不限于源代码、文档、二进制发行物及其修改版）由作者个人所有。
未经作者书面授权，任何个人或组织不得复制、修改、分发、出售、再许可、合并
或以其他方式将本框架用于商业或非商业用途。

免责声明：
本框架按“现状”提供，作者不对适销性、适用性或不侵权作出任何明示或暗示的保证。
在任何情况下，因使用或无法使用本框架而导致的任何直接或间接损失，作者概不负责。

联系作者以获取授权与支持：
Email: deovolenterin@gmail.com
*/

public static class UIEvents
{
    // 基础导航事件
    public static event Action<string> OnScreenPushed;
    public static event Action<string> OnScreenPopped;
    public static event Action<string, string> OnScreenReplaced;
    public static event Action OnNavigatedHome;
    
    // 过渡事件
    public static event Action<string> OnTransitionStart;
    public static event Action<string> OnTransitionEnd;
    
    // 内部通知方法
    internal static void NotifyScreenPushed(string screenId) 
        => OnScreenPushed?.Invoke(screenId);
    
    internal static void NotifyScreenPopped(string screenId) 
        => OnScreenPopped?.Invoke(screenId);
    
    internal static void NotifyScreenReplaced(string fromId, string toId) 
        => OnScreenReplaced?.Invoke(fromId, toId);
    
    internal static void NotifyNavigatedHome() 
        => OnNavigatedHome?.Invoke();
    
    internal static void NotifyTransitionStart(string screenId) 
        => OnTransitionStart?.Invoke(screenId);
    
    internal static void NotifyTransitionEnd(string screenId) 
        => OnTransitionEnd?.Invoke(screenId);
    
    // 清理所有事件（用于场景切换等情况）
    public static void ClearAllListeners()
    {
        OnScreenPushed = null;
        OnScreenPopped = null;
        OnScreenReplaced = null;
        OnNavigatedHome = null;
        OnTransitionStart = null;
        OnTransitionEnd = null;
    }
}