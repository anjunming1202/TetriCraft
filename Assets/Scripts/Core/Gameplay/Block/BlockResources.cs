using System.Collections.Generic;
using UnityEngine;

public class BlockResources : MonoBehaviour
{
    static public Dictionary<BlockID, GameObject> BlockIndexer;

    public List<GameObject> blockPrefabList = new List<GameObject>();

    private void Awake()
    {
        BlockIndexer = new Dictionary<BlockID, GameObject>();
        foreach (var prefab in blockPrefabList)
        {
            Block block = prefab.GetComponent<Block>();
            BlockIndexer.Add(block.ID, prefab);
        }
    }
}

// TODO: make it a singleton dontdestroyonload general PrefabManager