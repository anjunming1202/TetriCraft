using UnityEngine;

public class RedstoneBlock : Block
{
    public override BlockID ID => BlockID.RedstoneBlock;

    public override bool isCharged { get { return true; } }

    public override void OnDischarged(Vector2Int sourcePosition)
    {
        return;
    }

    public override void OnSpawn(MapManager map)
    {
        base.OnSpawn(map);
        OnCharged(GridPosition);
    }
}
