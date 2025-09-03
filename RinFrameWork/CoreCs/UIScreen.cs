using UnityEngine;

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

[RequireComponent(typeof(CanvasGroup))]
public class UIScreen : MonoBehaviour
{
    [SerializeField] private string screenId;
    [Header("Transition Override")]
    [Tooltip("界面专属的过渡配置，留空则使用 UIRouter 的默认配置")]
    [SerializeField] private TransitionConfig overrideTransition;
    public string ScreenId 
    { 
        get => screenId;
        set => screenId = value;
    }
    
    // 缓存引用
    private CanvasGroup _canvasGroup;
    public CanvasGroup CanvasGroup
    {
        get
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            return _canvasGroup;
        }
    }

    // 叠加层（如弹窗）不暂停下层界面
    public virtual bool IsOverlay => false;
    public TransitionConfig OverrideTransition => overrideTransition;

    // 生命周期方法
    public virtual void OnScreenCreate() { }
    public virtual void OnEnter(object param) { }
    public virtual void OnPause() { }
    public virtual void OnResume() { }
    public virtual void OnExit() { }
    public virtual void OnScreenDestroy() { }
    public virtual void OnRefresh(object param) { }
    
    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(screenId))
            screenId = GetType().Name;
        OnScreenCreate();
    }
    
    protected virtual void OnDestroy()
    {
        OnScreenDestroy();
    }
}