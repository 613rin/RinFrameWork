using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UIEvent
{
}

// 设置标签文本
public struct SetLabelText: EventBus.IContextEvent
{
    
    public string key;    // 标签键（可用对象名）
    public string text;   // 要设置的文本
    public int contextId { get; set; }
}

// 组内只保留指定 key 激活，其余隐藏
public struct ActivateOnlyInGroup : EventBus.IContextEvent
{
    public int contextId { get; set; } // 作用域
    public string group;     // 组名（如 "MainPanels"）
    public string[] activeKeys; // 需保持激活的成员键；为空或0长度=全部隐藏
}

// 新增：设置多个标签
public struct SetMultipleLabels : EventBus.IContextEvent
{
    public int contextId { get; set; }
    public Dictionary<string, string> labelTexts; // key -> text
}

// 新增：通用的UI更新事件
public struct UIDataChanged : EventBus.IContextEvent
{
    public int contextId { get; set; }
    public string dataKey;
    public object newValue;
}

// 新增：按钮点击事件（带数据）
public struct ButtonClickedWithData : EventBus.IContextEvent
{
    public int contextId { get; set; }
    public string buttonName;
    public string data;
}
