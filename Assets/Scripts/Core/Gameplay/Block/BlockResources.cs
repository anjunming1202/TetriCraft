using System.Collections.Generic;
using UnityEngine;

public class BlockResources : PersistentSingleton<BlockResources>
{
    static public Dictionary<BlockID, GameObject> BlockIndexer;

    public List<GameObject> blockPrefabList = new List<GameObject>();

    protected override void Awake()
    {
        base.Awake();

        BlockIndexer = new Dictionary<BlockID, GameObject>();
        foreach (var prefab in blockPrefabList)
        {
            Block block = prefab.GetComponent<Block>();
            BlockIndexer.Add(block.ID, prefab);
        }
    }
}