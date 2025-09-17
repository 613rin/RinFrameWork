using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CanvasGroup))]
public class UIScreen: MonoBehaviour
{
    [SerializeField] private string screenId;
    [Header("自定义过渡设置")]
    [Tooltip("界面专属的过渡配置，留空则使用 UIRouter 的默认配置")]
    [SerializeField] private TransitionConfig overrideTransition;
    
    
    // 添加：存储订阅的事件，用于自动取消订阅
    private List<System.Action> _unsubscribeActions = new List<System.Action>();
   
    protected int ContextId => Context != null ? Context.contextId : 0;
    
   
    #region 各种属性Get/Set

      #region CanvasGroup

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

       #endregion

      #region ScreenId

       public string ScreenId 
       { 
           get => screenId;
           set => screenId = value;
       }

       #endregion

       #region UIContext

       // Context支持
       private UIContext _context;
       protected UIContext Context
       {
           get
           {
               if (_context == null)
                   _context = GetComponent<UIContext>();
               return _context;
           }
       }
    
       // 获取当前界面的ContextId

       #endregion
   

    
    #endregion
    
    // 叠加层（如弹窗）不暂停下层界面
    public virtual bool IsOverlay => false;
    public TransitionConfig OverrideTransition => overrideTransition;

  
    // 生命周期方法
    public virtual void OnScreenCreate() { }
    public virtual void OnEnter(object param) { }
    public virtual void OnPause() { }
    public virtual void OnResume() { }
    public virtual void OnScreenDestroy() { }
    public virtual void OnRefresh(object param) { }
    public virtual void OnExit() { UnsubscribeAll(); } // 自动取消所有订阅
    
    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(screenId))
            screenId = GetType().Name;
            
        // 确保有Context组件
        if (_context == null)
        {
            _context = GetComponent<UIContext>();
            if (_context == null)
            {
                _context = gameObject.AddComponent<UIContext>();
                _context.EnsureId();
            }
        }
        
        OnScreenCreate();
    }
    
    protected virtual void OnDestroy()
    {
        UnsubscribeAll(); // 确保清理
        OnScreenDestroy();
    }
    
    #region EventBus 订阅方法(接受事件)
    // 订阅事件（自动处理Context过滤）
    protected void Subscribe<T>(System.Action<T> handler) where T : EventBus.IContextEvent
    {
        System.Action<T> wrappedHandler = (evt) =>
        {
            // 只处理属于当前Context的事件
            if (evt.contextId == ContextId || evt.contextId == 0)
            {
                handler(evt);
            }
        };
        
        EventBus.Subscribe(wrappedHandler);
        _unsubscribeActions.Add(() => EventBus.Unsubscribe(wrappedHandler));
    }
    
    // 订阅全局事件（不需要Context的事件）
    protected void SubscribeGlobal<T>(System.Action<T> handler)
    {
        EventBus.Subscribe(handler);
        _unsubscribeActions.Add(() => EventBus.Unsubscribe(handler));
    }
    
    // 手动取消所有订阅
    protected void UnsubscribeAll()
    {
        foreach (var unsubscribe in _unsubscribeActions)
        {
            unsubscribe?.Invoke();
        }
        _unsubscribeActions.Clear();
    }
    
    #endregion
    
    #region EventBus  发布方法(发送事件)
    
    // 发布带作用域的事件
    protected void PublishEvent<T>(T evt) where T : EventBus.IContextEvent
    {
        evt.contextId = ContextId;
        EventBus.Publish(evt);
    }
    
    // 设置单个标签
    protected void SetLabel(string key, string text)
    {
        EventBus.Publish(new SetLabelText
        {
            contextId = ContextId,
            key = key,
            text = text
        });
    }
    
    // 设置多个标签
    protected void SetLabels(params (string key, string text)[] labels)
    {
        var dict = new Dictionary<string, string>();
        foreach (var (key, text) in labels)
        {
            dict[key] = text;
        }
        
        EventBus.Publish(new SetMultipleLabels
        {
            contextId = ContextId,
            labelTexts = dict
        });
    }
    
    // 发布数据变化
    protected void NotifyDataChanged(string dataKey, object newValue)
    {
        EventBus.Publish(new UIDataChanged
        {
            contextId = ContextId,
            dataKey = dataKey,
            newValue = newValue
        });
    }
    
    #endregion
}