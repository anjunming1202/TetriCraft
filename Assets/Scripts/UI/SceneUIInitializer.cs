using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SceneUIInitializer
/// - Inspector: set an array of PrefabEntry (key + prefab + options)
/// - On Start (or when triggered) instantiate each prefab, register to UIManager, and optionally show it.
/// - On Destroy / OnDisable: hide + unregister + cleanup instantiated objects.
/// - Supports synchronous instantiation; can be extended to async Addressables if needed.
/// </summary>
public class SceneUIInitializer : Singleton<SceneUIInitializer>
{
    [Serializable]
    public class PrefabEntry
    {
        public string key;
        public BasePanel prefab;
        public Canvas canvas;
        public bool showOnStart = true;
        public bool destroyOnSceneUnload = true; // if reparented and persistent, destroy on cleanup
    }

    public PrefabEntry[] prefabEntries;

    // internal tracking
    class InstInfo
    {
        public PrefabEntry entry;
        public BasePanel instance;
        public Transform originalParent;
    }
    Dictionary<string, InstInfo> instances = new Dictionary<string, InstInfo>();

    public float waitForUIManagerTimeout = 5f; // seconds

    /// <summary>
    /// get instance by key (for Scene scripts/controllers).
    /// </summary>
    public BasePanel GetInstance(string key)
    {
        var it = instances[key];
        return it != null ? it.instance : null;
    }

    protected virtual void InitInstance(PrefabEntry entry, BasePanel panel) { }

    private IEnumerator Start()
    {
        // Wait one frame for Awake/Start ordering
        yield return null;

        // Wait for UIManager instance ready
        float start = Time.realtimeSinceStartup;
        while (UIManager.Instance == null)
        {
            if (Time.realtimeSinceStartup - start > waitForUIManagerTimeout)
            {
                Debug.LogWarning($"SceneUIInitializer: UIManager not found after {waitForUIManagerTimeout}s - abort registration.");
                yield break;
            }
            yield return null;
        }

        // Instantiate & register
        foreach (var entry in prefabEntries)
        {
            if (entry == null || entry.prefab == null) continue;
            try
            {
                // Instantiate under this registrar so it's organized; may reparent later
                var go = Instantiate(entry.prefab, this.transform);
                go.name = entry.prefab.name; // keep name tidy

                var basePanel = go.GetComponent<BasePanel>();
                if (basePanel == null)
                {
                    Debug.LogWarning($"SceneUIInitializer: prefab '{entry.prefab.name}' has no BasePanel component.");
                }

                var info = new InstInfo
                {
                    entry = entry,
                    instance = go,
                    originalParent = go.transform.parent
                };
                instances.Add(entry.key, info);

                // Initialize instance
                InitInstance(entry, basePanel);

                // Register with UIManager
                UIManager.Instance.RegisterScenePanel(entry.key, basePanel, entry.canvas);

                // Optionally show
                if (entry.showOnStart)
                {
                    UIManager.Instance.ShowPanel<BasePanel>(entry.key);
                }

                Debug.Log($"SceneUIInitializer: Registered panel '{entry.key}' from prefab '{entry.prefab.name}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SceneUIInitializer: Exception instantiating prefab {entry.prefab.name}: {ex}");
            }
        }
    }

    private void OnDisable()
    {
        CleanupAll();
    }

    protected override void OnDestroy()
    {
        CleanupAll();
        base.OnDestroy();
    }

    private void CleanupAll()
    {
        // Hide + Unregister + Destroy or restore parent
        foreach (var info in instances.Values)
        {
            if (info == null || info.instance == null) continue;

            try
            {
                UIManager.Instance?.HidePanel(info.entry.key);
            }
            catch { }

            try
            {
                UIManager.Instance?.UnregisterScenePanel(info.entry.key);
            }
            catch { }

            // If destroyOnSceneUnload -> destroy
            if (info.entry.destroyOnSceneUnload)
            {
                if (Application.isPlaying) Destroy(info.instance);
                else DestroyImmediate(info.instance);
            }
            else
            {
                // try restore original parent (if valid)
                try
                {
                    info.instance.transform.SetParent(info.originalParent, false);
                    // optionally set active state
                    // info.instance.SetActive(false);
                }
                catch { }
            }
        }

        instances.Clear();
    }
}
