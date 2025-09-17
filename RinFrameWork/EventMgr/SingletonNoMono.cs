using UnityEngine;

public abstract class SingletonNoMono<T> where T:class, new()
{
    private static T _instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                }
            }
            return _instance;
        }
    }

    protected SingletonNoMono()
    {
        // Constructor is protected to prevent instantiation from outside
    }
}

