using UnityEngine;

public class Water : FluidBlock
{
    [HideInInspector] public override BlockID ID => BlockID.Water;

    public override void OnLockdown(MapManager map)
    {
        base.OnLockdown(map);
        MapManager.WaterManager.SpawnElement(GridPosition.x, GridPosition.y, 0f, 1f);
        Remove(map);
    }
}