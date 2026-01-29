using UnityEngine;

/// <summary>
/// A static instance similar to a singleton, it overrides the current instance.
/// </summary>
public abstract class StaticInstance<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }
    protected virtual void Awake() => Instance = this as T;

    protected virtual void OnApplicationQuit()
    {
        Instance = null;
        Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        Debug.Log($"Destroyed static instance {gameObject.name}");
    }
}

/// <summary>
/// A basic singleton, it destroys any new versions and leaves the original instance intact.
/// </summary>
public abstract class Singleton<T> : StaticInstance<T> where T : MonoBehaviour
{
    protected override void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"Singleton<{typeof(T)}>: try to instantiate a second singleton object {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        base.Awake();
        // using service locator
        ServiceLocator.Register(Instance);
    }

    protected override void OnDestroy()
    {
        if (Instance == this)
        {
            ServiceLocator.Unregister(Instance);
        }
        Debug.Log($"Destroyed singleton {gameObject.name}");
    }
}

/// <summary>
/// A persistent singleton (DontDestroyOnLoad), it destroys any new versions and leaves the original instance intact.
/// </summary>
public abstract class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour
{
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    protected override void OnDestroy()
    {
        Debug.Log($"Destroyed persistent singleton {gameObject.name}");
    }
}