using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
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
public class UIRouter : MonoBehaviour
{
    //饿汉单例模式
    private static UIRouter _instance;
    public static UIRouter Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIRouter>();
                if (_instance == null)
                {
                    var go = new GameObject("UIRouter");
                    _instance = go.AddComponent<UIRouter>();
                }
            }
            return _instance;
        }
    }

    [Header("设置")]
    [SerializeField] private Transform uiRoot;
    [SerializeField] private UIRegistry registry;
    [Header("首页/待机页")]
    [SerializeField] private string homeScreenId = "Home";
    
    [Header("Transition")]
    [SerializeField] private TransitionConfig transitionConfig;
    
    private IScreenTransition _transition;
    private readonly Stack<UIScreen> _stack = new Stack<UIScreen>();
    private readonly Dictionary<string, UIScreen> _cache = new Dictionary<string, UIScreen>();
    private readonly Dictionary<string, IScreenTransition> _transitionCache = new Dictionary<string, IScreenTransition>();
    private bool _transitioning;

    public bool IsTransitioning => _transitioning;
    public int StackCount => _stack.Count;
    //Peek方法是将顶部元素拿出来看一眼然后放回去，并不删除栈顶元素
    public UIScreen CurrentScreen => _stack.Count > 0 ? _stack.Peek() : null;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeTransition();
        InitializeUIRoot();
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(homeScreenId))
            NavigateHome();
    }

    private void InitializeTransition()
    {
        if (transitionConfig != null)
            _transition = transitionConfig.CreateTransition();
        else
            _transition = new FadeTransition();
    }

    private void InitializeUIRoot()
    {
        if (uiRoot == null)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
                uiRoot = canvas.transform;
            else
            {
                var go = new GameObject("UICanvas");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<UnityEngine.UI.CanvasScaler>();
                go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                uiRoot = canvas.transform;
            }
        }
    }

    #region 导航方式/显示场景的方式(会吃场景栈的深度)

    public void Push(string screenId, object param = null)
    {
        if (_transitioning || string.IsNullOrEmpty(screenId)) return;
        StartCoroutine(PushRoutine(screenId, param));
    }

    public void Pop()
    {
        if (_transitioning || _stack.Count <= 1) return;
        StartCoroutine(PopRoutine());
    }

    public void PopToRoot()
    {
        if (_transitioning) return;
        NavigateHome();
    }

    public void Replace(string screenId, object param = null)
    {
        if (_transitioning || string.IsNullOrEmpty(screenId)) return;
        StartCoroutine(ReplaceRoutine(screenId, param));
    }

    public void NavigateHome(object param = null)
    {
        if (_transitioning) return;
        StartCoroutine(NavigateHomeRoutine(param));
    }

    public void NavigateTo(string screenId, object param = null)
    {
        if (_transitioning) return;
        
        if (_stack.Count > 0 && _stack.Peek().ScreenId == screenId)
        {
            _stack.Peek().OnRefresh(param);
            return;
        }
        
        if (IsInStack(screenId))
            StartCoroutine(PopToRoutine(screenId, param));
        else
            Push(screenId, param);
    }

    #endregion

    #region 优化的协程实现(优化栈操作和过渡动画)

    private IEnumerator PushRoutine(string screenId, object param)
    {
        _transitioning = true;
        UIEvents.NotifyTransitionStart(screenId);

        var current = _stack.Count > 0 ? _stack.Peek() : null;
        var next = GetOrCreateScreen(screenId);
        
        if (next == null)
        {
            Debug.LogError($"Failed to create screen: {screenId}");
            _transitioning = false;
            yield break;
        }

        // 获取界面专属的过渡效果
        var transition = GetTransitionForScreen(next);
        
        // Prepare
        PrepareScreen(next, transition);
        next.OnEnter(param);

        // Transition
        if (current != null && !next.IsOverlay)
        {
            current.OnPause();
            // 退出时使用当前界面的过渡配置
            var exitTransition = GetTransitionForScreen(current);
            yield return exitTransition.TransitionOut(current);
            current.gameObject.SetActive(false);
        }
        
        yield return transition.TransitionIn(next);

        // Finalize
        FinalizeScreen(next, transition);
        _stack.Push(next);
        
        UIEvents.NotifyScreenPushed(screenId);
        UIEvents.NotifyTransitionEnd(screenId);
        _transitioning = false;
    }

    private IEnumerator PopRoutine()
    {
        _transitioning = true;
    
        var top = _stack.Pop();
        var screenId = top.ScreenId;
        UIEvents.NotifyTransitionStart(screenId);
    
        // 使用顶部界面自己的过渡配置退出
        var exitTransition = GetTransitionForScreen(top);
    
        top.OnExit();
        yield return exitTransition.TransitionOut(top);
    
        ReleaseScreen(top);

        if (_stack.Count > 0)
        {
            var next = _stack.Peek();
            bool wasInactive = !next.gameObject.activeSelf;
        
            if (wasInactive)
            {
                // 使用下一个界面自己的过渡配置进入
                var enterTransition = GetTransitionForScreen(next);
                PrepareScreen(next, enterTransition);
                yield return enterTransition.TransitionIn(next);
                FinalizeScreen(next, enterTransition);
            }
        
            next.OnResume();
        }
    
        UIEvents.NotifyScreenPopped(screenId);
        UIEvents.NotifyTransitionEnd(screenId);
        _transitioning = false;
    }

    private IEnumerator ReplaceRoutine(string screenId, object param)
    {
        _transitioning = true;
        UIEvents.NotifyTransitionStart(screenId);

        UIScreen current = null;
        IScreenTransition exitTransition = null;
    
        if (_stack.Count > 0)
        {
            current = _stack.Pop();
            // 获取当前界面的退出过渡
            exitTransition = GetTransitionForScreen(current);
            current.OnExit();
        }

        var next = GetOrCreateScreen(screenId);
        if (next == null)
        {
            if (current != null) _stack.Push(current);
            _transitioning = false;
            yield break;
        }

        // 获取新界面的进入过渡
        var enterTransition = GetTransitionForScreen(next);
    
        PrepareScreen(next, enterTransition);
        next.OnEnter(param);

        if (current != null)
        {
            yield return exitTransition.TransitionOut(current);
            ReleaseScreen(current);
        }
    
        yield return enterTransition.TransitionIn(next);

        FinalizeScreen(next, enterTransition);
        _stack.Push(next);
    
        UIEvents.NotifyScreenReplaced(current?.ScreenId ?? "", screenId);
        UIEvents.NotifyTransitionEnd(screenId);
        _transitioning = false;
    }

    private IEnumerator NavigateHomeRoutine(object param)
    {
        _transitioning = true;
        UIEvents.NotifyTransitionStart(homeScreenId);

        // Clear stack with proper transitions
        while (_stack.Count > 0)
        {
            var screen = _stack.Pop();
            screen.OnExit();
        
            // 只对最后一个界面使用过渡动画，其他直接隐藏
            if (_stack.Count == 0)
            {
                var exitTransition = GetTransitionForScreen(screen);
                yield return exitTransition.TransitionOut(screen);
            }
            else
            {
                screen.gameObject.SetActive(false);
            }
        
            ReleaseScreen(screen);
        }

        // Push home with its own transition
        var home = GetOrCreateScreen(homeScreenId);
        if (home != null)
        {
            var homeTransition = GetTransitionForScreen(home);
            PrepareScreen(home, homeTransition);
            home.OnEnter(param);
            yield return homeTransition.TransitionIn(home);
            FinalizeScreen(home, homeTransition);
            _stack.Push(home);
        }

        UIEvents.NotifyNavigatedHome();
        UIEvents.NotifyTransitionEnd(homeScreenId);
        _transitioning = false;
    }

    private IEnumerator PopToRoutine(string targetId, object param)
    {
        _transitioning = true;
        UIEvents.NotifyTransitionStart(targetId);

        // Pop until target is on top
        while (_stack.Count > 1 && _stack.Peek().ScreenId != targetId)
        {
            var top = _stack.Pop();
            top.OnExit();
        
            // 可以选择是否为中间界面播放退出动画
            // 选项1：快速清理，不播放动画
            top.gameObject.SetActive(false);
        
            // 选项2：为每个界面播放退出动画（较慢）
            // var exitTransition = GetTransitionForScreen(top);
            // yield return exitTransition.TransitionOut(top);
        
            ReleaseScreen(top);
        }

        if (_stack.Count > 0 && _stack.Peek().ScreenId == targetId)
        {
            var target = _stack.Peek();
            bool wasInactive = !target.gameObject.activeSelf;
        
            if (wasInactive)
            {
                var targetTransition = GetTransitionForScreen(target);
                PrepareScreen(target, targetTransition);
                yield return targetTransition.TransitionIn(target);
                FinalizeScreen(target, targetTransition);
            }
        
            target.OnResume();
            target.OnRefresh(param);
        }

        UIEvents.NotifyTransitionEnd(targetId);
        _transitioning = false;
    }

    #endregion

    #region 场景管理器

    private UIScreen GetOrCreateScreen(string screenId)
    {
        // Check cache first
        if (_cache.TryGetValue(screenId, out var cached) && cached != null)
            return cached;

        // Get config from registry
        var config = registry?.GetConfig(screenId);
        if (config == null)
        {
            Debug.LogError($"No configuration found for screen: {screenId}");
            return null;
        }

        // Create instance
        var instance = Instantiate(config.prefab, uiRoot);
        var screen = instance.GetComponent<UIScreen>();
        
        if (screen == null)
        {
            Debug.LogError($"Prefab for {screenId} has no UIScreen component");
            Destroy(instance);
            return null;
        }

        screen.ScreenId = screenId;

        // Handle caching
        if (config.persistent || config.cacheAfterFirstUse)
        {
            _cache[screenId] = screen;
        }

        return screen;
    }

    private void ReleaseScreen(UIScreen screen)
    {
        if (screen == null) return;

        var config = registry?.GetConfig(screen.ScreenId);
        bool shouldCache = config != null && (config.persistent || config.cacheAfterFirstUse);

        if (shouldCache && _cache.ContainsKey(screen.ScreenId))
        {
            screen.gameObject.SetActive(false);
        }
        else
        {
            Destroy(screen.gameObject);
            _cache.Remove(screen.ScreenId);
        }
    }

    private void PrepareScreen(UIScreen screen)
    {
        screen.transform.SetAsLastSibling();
        screen.gameObject.SetActive(true);
        _transition.PrepareEnter(screen);
    }

    private void FinalizeScreen(UIScreen screen)
    {
        _transition.FinalizeEnter(screen);
    }
    
    private void PrepareScreen(UIScreen screen, IScreenTransition transition)
    {
        screen.transform.SetAsLastSibling();
        screen.gameObject.SetActive(true);
        transition.PrepareEnter(screen);
    }
    
    private void FinalizeScreen(UIScreen screen, IScreenTransition transition)
    {
        transition.FinalizeEnter(screen);
    }
    


    private IScreenTransition GetTransitionForScreen(UIScreen screen)
    {
        var screenId = screen.ScreenId;
    
        // 检查缓存
        if (_transitionCache.TryGetValue(screenId, out var cached))
            return cached;
    
        // 创建新的过渡实例
        IScreenTransition transition;
        if (screen.OverrideTransition != null)
        {
            transition = screen.OverrideTransition.CreateTransition();
        }
        else
        {
            transition = _transition ?? new FadeTransition();
        }
    
        // 缓存过渡实例（如果界面是持久的）
        var config = registry?.GetConfig(screenId);
        if (config != null && (config.persistent || config.cacheAfterFirstUse))
        {
            _transitionCache[screenId] = transition;
        }
    
        return transition;
    }

    #endregion

    #region 调试Debug方法

    public bool IsInStack(string screenId)
    {
        return _stack.Any(s => s.ScreenId == screenId);
    }

    public List<string> GetStackScreenIds()
    {
        return _stack.Select(s => s.ScreenId).ToList();
    }

    public UIScreen GetScreen(string screenId)
    {
        return _stack.FirstOrDefault(s => s.ScreenId == screenId);
    }

    #endregion
}