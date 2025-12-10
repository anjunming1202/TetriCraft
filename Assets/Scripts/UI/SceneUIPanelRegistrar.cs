using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScenePrefabUIRegistrar
/// - Inspector: set an array of PrefabEntry (key + prefab + options)
/// - On Start (or when triggered) instantiate each prefab, register to UIManager, and optionally show it.
/// - On Destroy / OnDisable: hide + unregister + cleanup instantiated objects.
/// - Supports synchronous instantiation; can be extended to async Addressables if needed.
/// </summary>
public class ScenePrefabUIRegistrar : Singleton<ScenePrefabUIRegistrar>
{
    [Serializable]
    public class PrefabEntry
    {
        public string key;
        public GameObject prefab;
        public bool showOnStart = true;
        public bool reparentToUIRoot = true;
        public bool destroyOnSceneUnload = true; // if reparented and persistent, destroy on cleanup
    }

    public PrefabEntry[] prefabEntries;

    // internal tracking
    class InstInfo
    {
        public PrefabEntry entry;
        public GameObject instance;
        public Transform originalParent;
    }
    List<InstInfo> instances = new List<InstInfo>();

    public float waitForUIManagerTimeout = 5f; // seconds

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
                Debug.LogWarning($"ScenePrefabUIRegistrar: UIManager not found after {waitForUIManagerTimeout}s - abort registration.");
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
                    Debug.LogWarning($"ScenePrefabUIRegistrar: prefab '{entry.prefab.name}' has no BasePanel component.");
                }

                var info = new InstInfo
                {
                    entry = entry,
                    instance = go,
                    originalParent = go.transform.parent
                };
                instances.Add(info);

                // Register with UIManager
                UIManager.Instance.RegisterScenePanel(entry.key, basePanel);

                // Optionally reparent to UIManager root so ordering/modal works as expected
                if (entry.reparentToUIRoot && UIManager.Instance.rootCanvas != null)
                {
                    go.transform.SetParent(UIManager.Instance.rootCanvas.transform, false);
                }

                // Optionally show
                if (entry.showOnStart)
                {
                    UIManager.Instance.ShowPanel<BasePanel>(entry.key);
                }

                Debug.Log($"ScenePrefabUIRegistrar: Registered panel '{entry.key}' from prefab '{entry.prefab.name}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ScenePrefabUIRegistrar: Exception instantiating prefab {entry.prefab.name}: {ex}");
            }
        }
    }

    private void OnDisable()
    {
        CleanupAll();
    }

    private void OnDestroy()
    {
        CleanupAll();
    }

    private void CleanupAll()
    {
        // Hide + Unregister + Destroy or restore parent
        foreach (var info in instances)
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

            // If reparented and destroyOnSceneUnload -> destroy
            if (info.entry.reparentToUIRoot && info.entry.destroyOnSceneUnload)
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

    /// <summary>
    /// Optional api: get instance by key (for Scene scripts/controllers).
    /// </summary>
    public GameObject GetInstance(string key)
    {
        var it = instances.Find(i => i.entry != null && i.entry.key == key);
        return it != null ? it.instance : null;
    }
}
