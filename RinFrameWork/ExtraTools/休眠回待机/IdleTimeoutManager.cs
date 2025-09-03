using System.Collections;
using UnityEngine;

public class IdleTimeoutManager : MonoBehaviour
{
    [Header("Timeout Settings")]
    [SerializeField] private float idleTimeoutSeconds = 300f; // 默认5分钟
    [SerializeField] private string timeoutScreenId = "Home"; // 超时后跳转的界面
    [SerializeField] private bool useNavigateTo = true; // 使用NavigateTo而不是Push
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private float _idleTime = 0f;
    private bool _isActive = false;
    
    private void OnEnable()
    {
        // 场景激活时开始计时
        _idleTime = 0f;
        _isActive = true;
        
        if (showDebugInfo)
        {
            Debug.Log($"[IdleTimeoutManager] Started monitoring on {gameObject.name}");
        }
    }
    
    private void OnDisable()
    {
        // 场景禁用时停止
        _isActive = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"[IdleTimeoutManager] Stopped monitoring on {gameObject.name}");
        }
    }
    
    private void Update()
    {
        if (!_isActive) return;
        
        // 检查是否有鼠标点击
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            // 重置计时
            _idleTime = 0f;
            
            if (showDebugInfo)
            {
                Debug.Log("[IdleTimeoutManager] Click detected, timer reset");
            }
        }
        else
        {
            // 没有点击，累加时间
            _idleTime += Time.deltaTime;
            
            // 检查是否超时
            if (_idleTime >= idleTimeoutSeconds)
            {
                OnTimeout();
            }
        }
    }
    
    private void OnTimeout()
    {
        if (showDebugInfo)
        {
            Debug.Log($"[IdleTimeoutManager] Timeout! Navigating to: {timeoutScreenId}");
        }
        
        // 重置计时器，避免重复触发
        _idleTime = 0f;
        
        var router = UIRouter.Instance;
        if (router != null && !router.IsTransitioning)
        {
            // 检查是否已经在目标界面
            if (router.CurrentScreen != null && router.CurrentScreen.ScreenId == timeoutScreenId)
            {
                if (showDebugInfo)
                {
                    Debug.Log("[IdleTimeoutManager] Already on timeout screen");
                }
                return;
            }
            
            // 执行跳转
            if (useNavigateTo)
            {
                router.NavigateTo(timeoutScreenId);
            }
            else
            {
                router.NavigateHome();
            }
        }
    }
    
    // 获取剩余时间（用于UI显示）
    public float GetRemainingTime()
    {
        return Mathf.Max(0, idleTimeoutSeconds - _idleTime);
    }
    
    // 获取已空闲时间
    public float GetIdleTime()
    {
        return _idleTime;
    }
    
    // 手动重置计时器
    public void ResetTimer()
    {
        _idleTime = 0f;
    }
    
    // 手动触发超时（测试用）
    [ContextMenu("Force Timeout")]
    public void ForceTimeout()
    {
        OnTimeout();
    }
}