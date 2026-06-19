using BlockSystem;
using System.Collections.Generic;
using UnityEngine;

public class Glass : Block
{
    [HideInInspector] public override BlockID ID => BlockID.Glass;
    public override bool CanBeReplacedBy(Block block)
    {
        if (!block.isInMap)
            return false;
        if (!isLocked || block.isLocked)
            return false;
        if (supportableBlock.Contains(block.ID))
            return false;
        return true;
    }

    public override ReplacementDisposition GetReplacementDisposition(Block incoming)
    {
        return ReplacementDisposition.Destroy;
    }

    private readonly List<BlockID> supportableBlock = new List<BlockID>()
    {
        BlockID.Glass,
        BlockID.Water,
        BlockID.Lava,
    };
}
