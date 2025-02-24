using UnityEngine;

// Package unit of a block, connecting model & view (block data & rendered instance)
public class BlockObject
{
    public BlockObject(Block block)
    {
        this.block = block;
        this.blockObject = new GameObject();
    }

    // Model (block) and View (game object) of block
    private readonly Block block;
    public GameObject blockObject;

    
}
