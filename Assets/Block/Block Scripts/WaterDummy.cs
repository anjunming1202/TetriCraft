using UnityEngine;

public class WaterDummy : Block
{
    [HideInInspector] public override BlockID ID => BlockID.WaterDummy;

    public override bool OnTryReplacedBy(Block block)
    {
        return true;
    }
}