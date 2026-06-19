using UnityEngine;

public class RedstoneBlock : Block, IRedstonePowerSource
{
    public override BlockID ID => BlockID.RedstoneBlock;

    // RedstoneBlock always emits power in all directions
    public bool PowersPosition(Vector2Int myPos, Vector2Int neighborPos) => true;

    public override void OnPostSpawned()
    {
        base.OnPostSpawned();
        // Notify adjacent blocks that a power source appeared
        TriggerSelfNCUpdate();
    }
}
