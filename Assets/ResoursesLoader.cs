using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Automatically load all persistent resources when instatiated
/// </summary>
public class ResoursesLoader : PersistentSingleton<ResoursesLoader>
{
    [SerializeField] GameObject[] orderedPersistentGameObjectPrefabs;

    protected override void Awake()
    {
        base.Awake();
        InstantiateAll();
    }

    public void InstantiateAll()
    {
        // avoid instantiation dependency problems
        foreach (GameObject prefab in orderedPersistentGameObjectPrefabs)
        {
            if (prefab) Instantiate(prefab);
        }
    }
}
