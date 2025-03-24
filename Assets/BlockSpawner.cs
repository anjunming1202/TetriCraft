using Unity.VisualScripting;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    static public BlockSpawner Instance;

    private void Awake()
    {
        Instance = this;
    }

    public Block NewBlock(BlockID blockID)
    {
        GameObject blockObject = GameObject.Instantiate(BlockResources.BlockIndexer[blockID]);
        blockObject.transform.SetParent(this.transform);
        return blockObject.GetComponent<Block>();
    }

    public void Reparent(Transform from)
    {
        foreach (var obj in from.GetComponentsInChildren<Transform>())
        {
            if (obj == from)
                continue;
            obj.SetParent(this.transform);
        }
    }
}
