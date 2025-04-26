using UnityEngine;

public class Water : FluidBlock
{
    public override BlockID ID => BlockID.Water;

    public override void OnLockdown(MapManager map)
    {
        map.waterManager.AddFluidBlock(this);
        base.OnLockdown(map);
    }
}