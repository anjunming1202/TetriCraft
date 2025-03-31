using UnityEngine;

public class Glass : Block
{
    [HideInInspector] public override BlockID ID => BlockID.Glass;
    public override bool CanReplacedBy(Block block)
    {
        if (!isLocked || block.isLocked)
            return false;
        if (block.ID == BlockID.Glass)
            return false;
        return true;
    }
}
