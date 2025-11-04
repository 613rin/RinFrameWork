using System;
using UnityEngine;

public abstract class SingletonNonMono<T> where T : SingletonNonMono<T>
{
    // 注意：不是 readonly，方便在 SubsystemRegistration 时重置
    private static Lazy<T> _instance = new Lazy<T>(CreateInstance, true);

    public static T Instance => _instance.Value;

    protected SingletonNonMono()
    {
        // 可选：防重复构造防线（理论上不会触发）
        if (_constructed) throw new InvalidOperationException($"{typeof(T)} already constructed.");
        _constructed = true;
    }
    private static bool _constructed;

    private static T CreateInstance()
    {
        // 允许调用非 public 的构造函数
        return (T)Activator.CreateInstance(typeof(T), nonPublic: true);
    }

    // 若在 Project Settings > Editor 里关闭了 Domain Reload，进入播放模式前重置静态
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlaymode()
    {
        _constructed = false;
        _instance = new Lazy<T>(CreateInstance, true);
    }
}