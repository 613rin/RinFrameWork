using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
    private HashSet<string> _creatingScreens = new HashSet<string>();
    // 添加一个缓存来加速查找
    private Dictionary<string, HashSet<string>> _parentChildMapping = new Dictionary<string, HashSet<string>>();
    private bool _transitioning;

    public bool IsTransitioning => _transitioning;
    public int StackCount => _stack.Count;
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

    
    // 自动导航到首页
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
        //原本就写好了一个过度方法，如果没有配置过度方法，就用这个
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
    
    
    // 预加载某个界面及其所有依赖
    public void PreloadScreen(string screenId)
    {
        if (string.IsNullOrEmpty(screenId)) return;
    
        var screen = GetOrCreateScreen(screenId);
        if (screen != null)
        {
            screen.gameObject.SetActive(false);
            Debug.Log($"Preloaded screen: {screenId}");
        }
    }
    
    // 批量预加载
    public void PreloadScreens(params string[] screenIds)
    {
        foreach (var screenId in screenIds)
        {
            PreloadScreen(screenId);
        }
    }
    
    
    
    [ContextMenu("Validate UI Registry")]
    public void ValidateRegistry()
    {
        if (registry == null)
        {
            Debug.LogError("No UI Registry assigned!");
            return;
        }
    
        var allScreenIds = registry.GetAllScreenIds();
        var errors = new List<string>();
    
        foreach (var screenId in allScreenIds)
        {
            var config = registry.GetConfig(screenId);
        
            // 检查预制体
            if (config.prefab == null)
                errors.Add($"{screenId}: Missing prefab");
        
            // 检查父界面是否存在
            if (!string.IsNullOrEmpty(config.parentScreenId))
            {
                var parentConfig = registry.GetConfig(config.parentScreenId);
                if (parentConfig == null)
                    errors.Add($"{screenId}: Parent '{config.parentScreenId}' not found");
            }
        
            // 检查循环依赖
            if (HasCircularDependency(screenId, new HashSet<string>()))
                errors.Add($"{screenId}: Circular dependency detected");
        }
        
        
        
    
        if (errors.Count > 0)
        {
            Debug.LogError($"Registry validation failed with {errors.Count} errors:");
            foreach (var error in errors)
                Debug.LogError($"  - {error}");
        }
        else
        {
            Debug.Log("检验通过!");
        }
    }
    
    private bool HasCircularDependency(string screenId, HashSet<string> visited)
    {
        if (visited.Contains(screenId))
            return true;
    
        visited.Add(screenId);
    
        var config = registry.GetConfig(screenId);
        if (config != null && !string.IsNullOrEmpty(config.parentScreenId))
            return HasCircularDependency(config.parentScreenId, visited);
    
        return false;
    }

    #region 修复补丁(逐层确保激活,同父互斥)

    // 激活从顶层到目标的整条父链（确保父屏可见）
    private void EnsureAncestorsActive(string screenId)
    {
        if (registry == null || string.IsNullOrEmpty(screenId)) return;
    
        var config = registry.GetConfig(screenId);
        if (config == null) return;
    
        // ✅ 只处理父界面及其祖先
        if (!string.IsNullOrEmpty(config.parentScreenId))
        {
            // 递归激活父界面及其祖先
            var parentPath = registry.GetHierarchyPath(config.parentScreenId);
            foreach (var id in parentPath)
            {
                var s = GetOrCreateScreen(id);
                if (s != null && !s.gameObject.activeSelf)
                    s.gameObject.SetActive(true);
            }
        }
    }

    // 沿路径逐层做“同父互斥”，并对 Root 顶层也做互斥
    private void EnforceExclusivityAlongPath(string screenId)
    {
        if (registry == null || string.IsNullOrEmpty(screenId)) return;
        var path = registry.GetHierarchyPath(screenId); // 例如 [一级A, 二级X, 三级Y]

        // 路径上每一层：只保留本层节点，失活同父兄弟
        foreach (var id in path)
        {
            var cfg = registry.GetConfig(id);
            var parentId = cfg != null ? (cfg.parentScreenId ?? "") : "";
            DeactivateSiblings(parentId, exceptId: id);
        }

        // 根层（无父）也互斥：只保留路径的第一个（一级）
        if (path.Count > 0)
            DeactivateSiblings(parentId: "", exceptId: path[0]);
    }

   // 失活某个父节点下的所有兄弟屏（exceptId 除外）
    private void DeactivateSiblings(string parentId, string exceptId)
    {
        var allIds = registry.GetAllScreenIds();
        string pFilter = parentId ?? "";

        foreach (var id in allIds)
        {
            var cfg = registry.GetConfig(id);
            if (cfg == null) continue;

            string p = cfg.parentScreenId ?? "";
            if (p != pFilter) continue;
            if (id == exceptId) continue;

            if (_cache.TryGetValue(id, out var s) && s != null)
            {
                // 可选：忽略叠加层
                if (s.IsOverlay) continue;

                DeactivateRecursive(s);
            }
        }
    }

// 递归失活一个屏及其所有子孙（不销毁，仅 SetActive(false)）
    private void DeactivateRecursive(UIScreen s)
    {
        if (s == null) return;

        // 先失活所有子屏
        var children = GetChildScreens(s.ScreenId);
        foreach (var child in children)
            DeactivateRecursive(child);

        s.gameObject.SetActive(false);
    }

    #endregion
    
    

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

    #region 优化的协程实现

    private IEnumerator PushRoutine(string screenId, object param)
    {
        _transitioning = true;
        UIEvents.NotifyTransitionStart(screenId);

        var current = _stack.Count > 0 ? _stack.Peek() : null;
        var next = GetOrCreateScreen(screenId);
        if (next == null) { _transitioning = false; yield break; }

        // 确保父链激活
        EnsureAncestorsActive(screenId);

        var transition = GetTransitionForScreen(next);
        PrepareScreen(next, transition);
        next.OnEnter(param);

        // ✅ 修改：检查新界面是否是当前界面的子界面
        bool isChildOfCurrent = IsChildScreen(screenId, current?.ScreenId);

        if (current != null && !next.IsOverlay && !isChildOfCurrent)  // ✅ 添加判断
        {
            current.OnPause();
            var exitTransition = GetTransitionForScreen(current);
            yield return exitTransition.TransitionOut(current);
            current.gameObject.SetActive(false);
        }
        else if (current != null && isChildOfCurrent)  // ✅ 如果是子界面，只暂停父界面
        {
            current.OnPause();
            // 不失活父界面，因为子界面需要显示在父界面上
        }

        yield return transition.TransitionIn(next);

        FinalizeScreen(next, transition);
        _stack.Push(next);

        EnforceExclusivityAlongPath(screenId);

        UIEvents.NotifyScreenPushed(screenId);
        UIEvents.NotifyTransitionEnd(screenId);
        _transitioning = false;
    }

    /// <summary>
    /// 检查childId是否是parentId的子界面（直接或间接）
    /// </summary>
    private bool IsChildScreen(string childId, string parentId)
    {
        if (string.IsNullOrEmpty(childId) || string.IsNullOrEmpty(parentId))
            return false;
    
        var childConfig = registry?.GetConfig(childId);
        if (childConfig == null)
            return false;
    
        // 检查直接父子关系
        if (childConfig.parentScreenId == parentId)
            return true;
    
        // 检查间接父子关系（递归向上查找）
        var currentParentId = childConfig.parentScreenId;
        var visited = new HashSet<string>(); // 防止循环依赖
    
        while (!string.IsNullOrEmpty(currentParentId))
        {
            if (visited.Contains(currentParentId))
                break; // 检测到循环
        
            visited.Add(currentParentId);
        
            if (currentParentId == parentId)
                return true;
        
            var parentConfig = registry?.GetConfig(currentParentId);
            currentParentId = parentConfig?.parentScreenId;
        }
    
        return false;
    }
    
    private IEnumerator PopRoutine()
    {
        _transitioning = true;
    
        var top = _stack.Pop();
        var screenId = top.ScreenId;
        UIEvents.NotifyTransitionStart(screenId);
    
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

        // 新增：激活父链
        EnsureAncestorsActive(screenId);

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

        // 新增：互斥
        EnforceExclusivityAlongPath(screenId);

        UIEvents.NotifyScreenReplaced(current?.ScreenId ?? "", screenId);
        UIEvents.NotifyTransitionEnd(screenId);
        _transitioning = false;
    }

    private IEnumerator NavigateHomeRoutine(object param)
    {
        _transitioning = true;
        UIEvents.NotifyTransitionStart(homeScreenId);

        while (_stack.Count > 0)
        {
            var screen = _stack.Pop();
            screen.OnExit();
        
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

        var home = GetOrCreateScreen(homeScreenId);
        if (home != null)
        {
            var homeTransition = GetTransitionForScreen(home);
            PrepareScreen(home, homeTransition);
            home.OnEnter(param);
            yield return homeTransition.TransitionIn(home);
            FinalizeScreen(home, homeTransition);
            _stack.Push(home);
            // 新增：互斥（根层只保留 Home）
            EnforceExclusivityAlongPath(homeScreenId);
        }

        UIEvents.NotifyNavigatedHome();
        UIEvents.NotifyTransitionEnd(homeScreenId);
        _transitioning = false;
    }

    private IEnumerator PopToRoutine(string targetId, object param)
    {
        _transitioning = true;
        UIEvents.NotifyTransitionStart(targetId);

        while (_stack.Count > 1 && _stack.Peek().ScreenId != targetId)
        {
            var top = _stack.Pop();
            top.OnExit();
            top.gameObject.SetActive(false);
            ReleaseScreen(top);
        }

        if (_stack.Count > 0 && _stack.Peek().ScreenId == targetId)
        {
            var target = _stack.Peek();
            bool wasInactive = !target.gameObject.activeSelf;

            // 添加这一行：确保目标界面的所有父界面都被激活
            EnsureAncestorsActive(targetId);

            if (wasInactive)
            {
                var targetTransition = GetTransitionForScreen(target);
                PrepareScreen(target, targetTransition);
                yield return targetTransition.TransitionIn(target);
                FinalizeScreen(target, targetTransition);
            }

            target.OnResume();
            target.OnRefresh(param);

            // 互斥（保证回到 target 时，其他顶层/同层兄弟被失活）
            EnforceExclusivityAlongPath(targetId);
        }

        UIEvents.NotifyTransitionEnd(targetId);
        _transitioning = false;
    }

    #endregion

    #region 场景管理器 - 支持层级创建

    private UIScreen GetOrCreateScreen(string screenId)
    {
        // 检测循环依赖
        if (_creatingScreens.Contains(screenId))
        {
            Debug.LogError($"Circular dependency detected when creating screen: {screenId}");
            return null;
        }
    
        // ✅ 修改：获取配置，检查是否应该使用缓存
        var config = registry?.GetConfig(screenId);
        if (config == null)
        {
            Debug.LogError($"No configuration found for screen: {screenId}");
            return null;
        }
    
        // ✅ 如果设置了 destroyOnDeactivate，则不使用缓存（总是重新创建）
        if (!config.destroyOnDeactivate && _cache.TryGetValue(screenId, out var cached) && cached != null)
            return cached;
    
        // 标记正在创建
        _creatingScreens.Add(screenId);

        try
        {
            // 确定父节点
            Transform parent = DetermineParent(config);
            if (parent == null)
            {
                Debug.LogError($"Failed to determine parent for screen: {screenId}");
                return null;
            }

            // 创建实例
            var instance = Instantiate(config.prefab, parent);
            var screen = instance.GetComponent<UIScreen>();
    
            if (screen == null)
            {
                Debug.LogError($"Prefab for {screenId} has no UIScreen component");
                Destroy(instance);
                return null;
            }

            screen.ScreenId = screenId;
            UpdateParentChildMapping(screenId, config.parentScreenId);
    
            // 处理Context继承
            if (!string.IsNullOrEmpty(config.parentScreenId))
            {
                StartCoroutine(SetupContextInheritance(screen, config.parentScreenId));
            }
        
            // ✅ 修改：只有在不是 destroyOnDeactivate 时才缓存
            if (!config.destroyOnDeactivate && (config.persistent || config.cacheAfterFirstUse))
            {
                _cache[screenId] = screen;
            }

            return screen;
        }
        finally
        {
            _creatingScreens.Remove(screenId);
        }
    }
    
    // 添加新方法：延迟处理Context继承
    private IEnumerator SetupContextInheritance(UIScreen childScreen, string parentScreenId)
    {
        yield return null; // 等待一帧，确保父界面已创建
    
        var childContext = childScreen.GetComponent<UIContext>();
        if (childContext == null) yield break;
    
        // 从缓存中查找父界面
        if (_cache.TryGetValue(parentScreenId, out var parentScreen) && parentScreen != null)
        {
            var parentContext = parentScreen.GetComponent<UIContext>();
            if (parentContext != null && parentContext.contextId > 0)
            {
                childContext.Inherit(parentContext.contextId);
                Debug.Log($"[UIRouter] {childScreen.ScreenId} 继承了 {parentScreenId} 的Context: {parentContext.contextId}");
            }
        }
    }
    
    private void UpdateParentChildMapping(string screenId, string parentScreenId)
    {
        if (string.IsNullOrEmpty(parentScreenId)) return;
    
        if (!_parentChildMapping.ContainsKey(parentScreenId))
            _parentChildMapping[parentScreenId] = new HashSet<string>();
    
        _parentChildMapping[parentScreenId].Add(screenId);
    }
    
    private Transform DetermineParent(UIRegistry.ScreenConfig config)
    {
        // 如果没有指定父节点，使用根节点
        if (string.IsNullOrEmpty(config.parentScreenId))
            return uiRoot;
        
        // 递归创建父节点
        var parentScreen = GetOrCreateScreen(config.parentScreenId);
        if (parentScreen == null)
            return null;
        
        // 如果指定了父节点内的路径
        if (!string.IsNullOrEmpty(config.parentPath))
        {
            var targetTransform = parentScreen.transform.Find(config.parentPath);
            if (targetTransform != null)
                return targetTransform;
            else
            {
                Debug.LogWarning($"Path '{config.parentPath}' not found in parent '{config.parentScreenId}', using parent root");
                return parentScreen.transform;
            }
        }
        
        return parentScreen.transform;
    }
    
    // 获取某个界面的所有子界面
    public List<UIScreen> GetChildScreens(string parentScreenId)
    {
        var children = new List<UIScreen>();
        
        if (_parentChildMapping.TryGetValue(parentScreenId, out var childIds))
        {
            foreach (var childId in childIds)
            {
                if (_cache.TryGetValue(childId, out var screen) && screen != null)
                    children.Add(screen);
            }
        }

        #region  旧的实现

        // foreach (var kvp in _cache)
        // {
        //     var config = registry?.GetConfig(kvp.Key);
        //     if (config != null && config.parentScreenId == parentScreenId && kvp.Value != null)
        //     {
        //         children.Add(kvp.Value);
        //     }
        // }

        #endregion
      
        
        return children;
    }

    private void ReleaseScreen(UIScreen screen)
    {
        if (screen == null) return;
    
        // 先释放所有子界面
        var children = GetChildScreens(screen.ScreenId);
        foreach (var child in children)
        {
            ReleaseScreen(child);
        }

        var config = registry?.GetConfig(screen.ScreenId);
    
        // ✅ 修改：如果设置了 destroyOnDeactivate，总是销毁
        if (config != null && config.destroyOnDeactivate)
        {
            Debug.Log($"[UIRouter] 销毁界面（每次重新创建）: {screen.ScreenId}");
            Destroy(screen.gameObject);
            _cache.Remove(screen.ScreenId);
            return;
        }
    
        // 原有的缓存逻辑
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
    
        if (_transitionCache.TryGetValue(screenId, out var cached))
            return cached;
    
        IScreenTransition transition;
        if (screen.OverrideTransition != null)
        {
            transition = screen.OverrideTransition.CreateTransition();
        }
        else
        {
            transition = _transition ?? new FadeTransition();
        }
    
        var config = registry?.GetConfig(screenId);
        if (config != null && (config.persistent || config.cacheAfterFirstUse))
        {
            _transitionCache[screenId] = transition;
        }
    
        return transition;
    }

    #endregion

    #region 调试方法

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
    
    // 获取缓存中的界面（包括未在栈中的）
    public UIScreen GetCachedScreen(string screenId)
    {
        _cache.TryGetValue(screenId, out var screen);
        return screen;
    }
    
    // 打印层级结构
    [ContextMenu("Print UI Hierarchy")]
    public void PrintUIHierarchy()
    {
        Debug.Log("=== UI Hierarchy ===");
        var allScreenIds = registry?.GetAllScreenIds() ?? new List<string>();
        
        foreach (var screenId in allScreenIds)
        {
            var config = registry.GetConfig(screenId);
            if (string.IsNullOrEmpty(config.parentScreenId))
            {
                PrintScreenHierarchy(screenId, 0);
            }
        }
    }
    
    private void PrintScreenHierarchy(string screenId, int depth)
    {
        var indent = new string('-', depth * 2);
        var cached = _cache.ContainsKey(screenId) ? "[Cached]" : "";
        var inStack = IsInStack(screenId) ? "[In Stack]" : "";
        
        Debug.Log($"{indent}{screenId} {cached} {inStack}");
        
        // 打印子节点
        var allScreenIds = registry?.GetAllScreenIds() ?? new List<string>();
        foreach (var childId in allScreenIds)
        {
            var config = registry.GetConfig(childId);
            if (config != null && config.parentScreenId == screenId)
            {
                PrintScreenHierarchy(childId, depth + 1);
            }
        }
    }

    #endregion
}