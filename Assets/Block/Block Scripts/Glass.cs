using System.Collections.Generic;
using UnityEngine;

public class Glass : Block
{
    [HideInInspector] public override BlockID ID => BlockID.Glass;
    public override bool CanBeReplacedBy(Block block)
    {
        if (!isLocked || block.isLocked)
            return false;
        if (supportableBlock.Contains(block.ID))
            return false;
        return true;
    }

    public override void OnReplacedBy(Block block)
    {
        map.DestroyBlock(this);
    }

    private List<BlockID> supportableBlock = new List<BlockID>()
    {
        BlockID.Glass,
        BlockID.Water,
        BlockID.Lava,
    };
}
