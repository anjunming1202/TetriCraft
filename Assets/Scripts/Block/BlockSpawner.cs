using Unity.VisualScripting;
using UnityEngine;

public static class BlockSpawner
{
    public static Block NewBlock(BlockID blockID)
    {
        GameObject blockObject = GameObject.Instantiate(BlockResources.BlockIndexer[blockID]);
        return blockObject.GetComponent<Block>();
    }
}
