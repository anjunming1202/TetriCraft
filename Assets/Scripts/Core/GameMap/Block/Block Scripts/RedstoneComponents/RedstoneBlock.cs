using UnityEngine;

public class RedstoneBlock : Block
{
    public override BlockID ID => BlockID.RedstoneBlock;

    public override void OnDischarged(Vector2Int sourcePosition)
    {
        return;
    }

    public override void OnPostSpawned()
    {
        base.OnPostSpawned();
        OnCharged(GridPosition);
    }
}
