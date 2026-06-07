using UnityEngine;

public class RedstoneBlock : Block
{
    public override BlockID ID => BlockID.RedstoneBlock;

    public override void OnDischarged(Vector2Int sourcePosition)
    {
        return;
    }

    public override void OnSpawn(MapManager map, int x, int y)
    {
        base.OnSpawn(map, x, y);
        OnCharged(GridPosition);
    }
}
