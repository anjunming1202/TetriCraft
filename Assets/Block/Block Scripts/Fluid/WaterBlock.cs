using UnityEngine;

public class WaterBlock : FluidBlock
{
    [HideInInspector] public override BlockID ID => BlockID.Water;

    public override void OnLockdown(MapManager map)
    {
        base.OnLockdown(map);
        MapManager.WaterManager.SpawnElement(GridPosition.x, FluidElement.Local2Level(GridPosition.y, 0));
        map.RemoveBlock(this);
    }
}