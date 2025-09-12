using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "UIRegistry", menuName = "UI System/Screen Registry")]
public class UIRegistry : ScriptableObject
{
    [System.Serializable]
    public class ScreenConfig
    {
        public string screenId;
        public GameObject prefab;
        [Tooltip("界面是否常驻内存")]
        public bool persistent;
        [Tooltip("首次使用后是否缓存")]
        public bool cacheAfterFirstUse;
        
        [Header("层级关系")]
        [Tooltip("父节点的screenId，留空则放在根节点下")]
        public string parentScreenId;
        [Tooltip("在父节点下的特定Transform路径，如 'Content/ScrollView'")]
        public string parentPath;
        
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(screenId) && prefab != null;
        }
    }

    [SerializeField] private List<ScreenConfig> screens = new List<ScreenConfig>();
    
    private Dictionary<string, ScreenConfig> _configMap;

    private void OnEnable()
    {
        RebuildConfigMap();
    }

    private void OnValidate()
    {
        RebuildConfigMap();
        ValidateHierarchy();
    }

    private void RebuildConfigMap()
    {
        _configMap = new Dictionary<string, ScreenConfig>();
        foreach (var config in screens.Where(s => s.IsValid()))
        {
            if (_configMap.ContainsKey(config.screenId))
            {
                Debug.LogWarning($"Duplicate screen ID in registry: {config.screenId}");
                continue;
            }
            _configMap[config.screenId] = config;
        }
    }
    
    private void ValidateHierarchy()
    {
        // 检查是否有循环依赖
        foreach (var config in screens.Where(s => s.IsValid()))
        {
            if (HasCircularDependency(config.screenId, new HashSet<string>()))
            {
                Debug.LogError($"Circular dependency detected for screen: {config.screenId}");
            }
        }
    }
    
    private bool HasCircularDependency(string screenId, HashSet<string> visited)
    {
        if (visited.Contains(screenId))
            return true;
            
        visited.Add(screenId);
        
        var config = GetConfig(screenId);
        if (config != null && !string.IsNullOrEmpty(config.parentScreenId))
        {
            return HasCircularDependency(config.parentScreenId, visited);
        }
        
        return false;
    }

    public ScreenConfig GetConfig(string screenId)
    {
        if (_configMap == null)
            RebuildConfigMap();
            
        _configMap.TryGetValue(screenId, out var config);
        return config;
    }

    public List<string> GetAllScreenIds()
    {
        if (_configMap == null)
            RebuildConfigMap();
            
        return _configMap.Keys.ToList();
    }

    public bool HasScreen(string screenId)
    {
        if (_configMap == null)
            RebuildConfigMap();
            
        return _configMap.ContainsKey(screenId);
    }
    
    // 获取从根到目标节点的完整路径
    public List<string> GetHierarchyPath(string screenId)
    {
        var path = new List<string>();
        var visited = new HashSet<string>();
        
        while (!string.IsNullOrEmpty(screenId))
        {
            if (visited.Contains(screenId))
            {
                Debug.LogError($"Circular dependency detected at: {screenId}");
                break;
            }
            
            visited.Add(screenId);
            path.Insert(0, screenId);
            
            var config = GetConfig(screenId);
            screenId = config?.parentScreenId;
        }
        
        return path;
    }
}