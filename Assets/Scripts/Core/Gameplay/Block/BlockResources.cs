using System.Collections.Generic;
using UnityEngine;

public class BlockResources : PersistentSingleton<BlockResources>
{
    [SerializeField] private List<GameObject> blockPrefabList = new List<GameObject>();

    static private Dictionary<BlockID, GameObject> BlockIndexer;

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

    static public GameObject GetPrefab(BlockID id)
    {
        return BlockIndexer[id]; 
    }

    static public Sprite GetSprite(BlockID id)
    {
        return BlockIndexer[id].GetComponent<SpriteRenderer>().sprite;
    }
}