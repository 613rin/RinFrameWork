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
}