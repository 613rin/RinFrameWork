using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 按钮组控制管理器（非单例版本）
/// 可以在场景中挂载多个，每个独立管理自己的按钮组
/// </summary>
public class ButtonControlManager : MonoBehaviour
{
    [System.Serializable]
    public class ButtonPair
    {
        [Tooltip("按钮名称（用于识别）")]
        public string buttonName = "按钮";

        [Tooltip("高亮状态按钮（点击无效果）")]
        public GameObject highlightButton;

        [Tooltip("暗/普通状态按钮（可点击，挂载脚本）")]
        public GameObject normalButton;
    }

    [Header("管理器标识")]
    [Tooltip("管理器名称（用于区分不同的管理器）")]
    public string managerName = "按钮组管理器";

    [Header("按钮配置")]
    [Tooltip("所有按钮对列表")]
    public List<ButtonPair> buttonPairs = new List<ButtonPair>();

    [Header("初始设置")]
    [Tooltip("初始激活的按钮索引（0=A, 1=B, 2=C...）")]
    public int initialActiveIndex = 0;

    [Tooltip("是否在Start时应用初始状态")]
    public bool applyInitialState = true;

    [Header("调试")]
    [Tooltip("显示调试日志")]
    public bool showDebugLog = true;

    // 当前激活的按钮索引
    private int currentActiveIndex = -1;

    private void Awake()
    {
        // 验证配置
        ValidateButtonPairs();
        
        if (showDebugLog)
            Debug.Log($"[{managerName}] 管理器初始化完成，管理 {buttonPairs.Count} 组按钮");
    }

    private void Start()
    {
        if (applyInitialState)
        {
            SwitchToButton(initialActiveIndex);
        }
    }

    /// <summary>
    /// 验证按钮对配置
    /// </summary>
    private void ValidateButtonPairs()
    {
        for (int i = 0; i < buttonPairs.Count; i++)
        {
            var pair = buttonPairs[i];
            
            if (pair.highlightButton == null)
            {
                Debug.LogError($"[{managerName}] 按钮对 {i} ({pair.buttonName}) 的高亮按钮为空！");
            }
            
            if (pair.normalButton == null)
            {
                Debug.LogError($"[{managerName}] 按钮对 {i} ({pair.buttonName}) 的暗按钮为空！");
            }
        }
    }

    /// <summary>
    /// 切换到指定索引的按钮
    /// </summary>
    public void SwitchToButton(int index)
    {
        if (index < 0 || index >= buttonPairs.Count)
        {
            Debug.LogError($"[{managerName}] 索引 {index} 超出范围！有效范围: 0 到 {buttonPairs.Count - 1}");
            return;
        }

        if (index == currentActiveIndex)
        {
            if (showDebugLog)
                Debug.Log($"[{managerName}] 按钮 {buttonPairs[index].buttonName} 已经是激活状态");
            return;
        }

        // 1. 将所有按钮设置为暗状态
        for (int i = 0; i < buttonPairs.Count; i++)
        {
            var pair = buttonPairs[i];
            
            if (pair.highlightButton != null)
                pair.highlightButton.SetActive(false);
            
            if (pair.normalButton != null)
                pair.normalButton.SetActive(true);
        }

        // 2. 将目标按钮设置为高亮状态
        var targetPair = buttonPairs[index];
        
        if (targetPair.highlightButton != null)
            targetPair.highlightButton.SetActive(true);
        
        if (targetPair.normalButton != null)
            targetPair.normalButton.SetActive(false);

        currentActiveIndex = index;

        if (showDebugLog)
            Debug.Log($"[{managerName}] 切换到按钮: {targetPair.buttonName} (索引: {index})");
    }

    /// <summary>
    /// 通过按钮名称切换
    /// </summary>
    public void SwitchToButton(string buttonName)
    {
        int index = buttonPairs.FindIndex(pair => pair.buttonName == buttonName);
        
        if (index >= 0)
        {
            SwitchToButton(index);
        }
        else
        {
            Debug.LogError($"[{managerName}] 找不到名称为 '{buttonName}' 的按钮！");
        }
    }

    /// <summary>
    /// 通过暗按钮GameObject切换（用于按钮点击事件）
    /// </summary>
    public void SwitchToButtonByNormal(GameObject normalButton)
    {
        int index = buttonPairs.FindIndex(pair => pair.normalButton == normalButton);
        
        if (index >= 0)
        {
            SwitchToButton(index);
        }
        else
        {
            Debug.LogError($"[{managerName}] 找不到暗按钮 '{normalButton.name}'！");
        }
    }

    /// <summary>
    /// 通过高亮按钮GameObject切换
    /// </summary>
    public void SwitchToButtonByHighlight(GameObject highlightButton)
    {
        int index = buttonPairs.FindIndex(pair => pair.highlightButton == highlightButton);
        
        if (index >= 0)
        {
            SwitchToButton(index);
        }
        else
        {
            Debug.LogError($"[{managerName}] 找不到高亮按钮 '{highlightButton.name}'！");
        }
    }

    /// <summary>
    /// 获取当前激活的按钮索引
    /// </summary>
    public int GetCurrentActiveIndex()
    {
        return currentActiveIndex;
    }

    /// <summary>
    /// 获取当前激活的按钮对
    /// </summary>
    public ButtonPair GetCurrentActivePair()
    {
        if (currentActiveIndex >= 0 && currentActiveIndex < buttonPairs.Count)
        {
            return buttonPairs[currentActiveIndex];
        }
        return null;
    }

    /// <summary>
    /// 获取当前激活的按钮名称
    /// </summary>
    public string GetCurrentActiveButtonName()
    {
        var pair = GetCurrentActivePair();
        return pair != null ? pair.buttonName : "无";
    }

    /// <summary>
    /// 重置所有按钮为暗状态
    /// </summary>
    public void ResetAllToNormal()
    {
        for (int i = 0; i < buttonPairs.Count; i++)
        {
            var pair = buttonPairs[i];
            
            if (pair.highlightButton != null)
                pair.highlightButton.SetActive(false);
            
            if (pair.normalButton != null)
                pair.normalButton.SetActive(true);
        }

        currentActiveIndex = -1;

        if (showDebugLog)
            Debug.Log($"[{managerName}] 所有按钮已重置为暗状态");
    }
}