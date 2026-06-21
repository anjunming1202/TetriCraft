using System.Collections.Generic;
using UnityEngine;

public class BlockResources : PersistentSingleton<BlockResources>
{
    // Path relative to any Resources/ folder, e.g. "Prefabs/Blocks"
    [SerializeField] private string blockPrefabsFolder = "Prefabs/Blocks";

    static private Dictionary<BlockID, GameObject> BlockIndexer;

    protected override void Awake()
    {
        base.Awake();

        BlockIndexer = new Dictionary<BlockID, GameObject>();
        GameObject[] prefabs = Resources.LoadAll<GameObject>(blockPrefabsFolder);
        if (prefabs.Length == 0)
            Debug.LogWarning($"[BlockResources] No prefabs found at Resources/{blockPrefabsFolder}");

        foreach (var prefab in prefabs)
        {
            Block block = prefab.GetComponent<Block>();
            if (block == null)
            {
                Debug.LogWarning($"[BlockResources] Prefab '{prefab.name}' has no Block component, skipped.");
                continue;
            }
            if (BlockIndexer.ContainsKey(block.ID))
            {
                Debug.LogWarning($"[BlockResources] Duplicate BlockID '{block.ID}' from prefab '{prefab.name}', skipped.");
                continue;
            }
            BlockIndexer.Add(block.ID, prefab);
        }
    }

    static public GameObject GetPrefab(BlockID id)
    {
        return BlockIndexer[id]; 
    }

    static public Sprite GetSprite(BlockID id)
    {
        return BlockIndexer[id].GetComponent<BlockRenderer>().DefaultSprite;
    }
}