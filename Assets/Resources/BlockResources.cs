using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.U2D;

// Load all block prefab from assets, stored in a global static list
public static class BlockResources
{
    // Folder Path for prefabs
    private static string prefabFolderPath = "Block Prefabs/";

    // Loaded Block Prefabs
    private static Dictionary<BlockType, GameObject> BlockPrefabs;


    // Get block prefab
    public static GameObject GetPrefab(BlockType type)
    {
        return BlockPrefabs[type];
    }

    // Load all block prefabs in a particular folder
    public static void LoadBlockPrefabs()
    {
        BlockPrefabs = new Dictionary<BlockType, GameObject>();

        LoadPrefab(BlockType.Null, "Null Block");
        LoadPrefab(BlockType.Cobblestone, "Cobblestone");


        Debug.Log($"Successfully Loaded {BlockPrefabs.Count} Prefabs!");
    }

    private static void LoadPrefab(BlockType type, string name)
    {
        Debug.Assert(!BlockPrefabs.ContainsKey(type), $"Already loaded {type} prefab!");

        GameObject prefab = Resources.Load<GameObject>(prefabFolderPath + name);

        if (prefab == null)
        {
            Debug.LogError($"Failed to load prefab: {prefabFolderPath + name}");
            return;
        }

        BlockPrefabs[type] = prefab;

        Debug.Log($"Loaded Prefab: {prefab.name}");
    }
}
