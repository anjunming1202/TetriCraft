using UnityEngine;

public class Glass : Block
{
    [HideInInspector] public override BlockID ID => BlockID.Glass;
    public override bool CanBeReplacedBy(Block block)
    {
        if (!isLocked || block.isLocked)
            return false;
        if (block.ID == BlockID.Glass)
            return false;
        return true;
    }

    public override void OnReplacedBy(MapManager map, Block block)
    {
        map.DestroyBlock(this);
    }
}
