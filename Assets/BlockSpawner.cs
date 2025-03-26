using Unity.VisualScripting;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    public Block NewBlock(BlockID blockID)
    {
        GameObject blockObject = GameObject.Instantiate(BlockResources.BlockIndexer[blockID]);
        blockObject.transform.SetParent(this.transform);
        return blockObject.GetComponent<Block>();
    }
}
