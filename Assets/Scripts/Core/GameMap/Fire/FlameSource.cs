using UnityEngine;

/// <summary>
/// Lava fire-spreading source. Mirrors Minecraft lava ignition behavior:
///   - Checks the 3-wide row at y+1 and the 5-wide row at y+2 (2D adaptation of 3×3 / 5×5).
///   - Occupied cell with lavaIgnitability > 0  →  side flame on that block.
///   - Empty cell whose neighbor has lavaIgnitability > 0  →  top flame on the block below.
/// This maps directly to how Flame.TryPlaceFireAt handles side vs. top flames.
/// </summary>
public class FlameSource : MapObject
{
    /// <summary>How many game ticks between ignition checks. Default 30 = 1.5 s at 20 Hz.</summary>
    [SerializeField] private int spreadInterval = 30;

    public Vector2Int position => BoundaryDataManager.GetBoundaryData(map.PlayerID).WorldToGrid(transform.position);

    private bool isDead;

    private static readonly Vector2Int[] CardinalDirs =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private void Start()
    {
        map = GetComponent<Block>().GetMap();
        int initialDelay = Random.Range(1, spreadInterval + 1);
        map.ScheduledTickManager.Schedule(OnScheduledTick, initialDelay, transform.position);
    }

    private void OnDestroy()
    {
        isDead = true;
    }

    private void OnScheduledTick()
    {
        if (isDead || this == null) return;
        Spread();
        map.ScheduledTickManager.Schedule(OnScheduledTick, spreadInterval, transform.position);
    }

    private void Spread()
    {
        Vector2Int lavaPos = position;

        // 2D adaptation: y+1 → ±1 in x (3 cells), y+2 → ±2 in x (5 cells)
        for (int dx = -1; dx <= 1; dx++)
            TryIgniteAt(new Vector2Int(lavaPos.x + dx, lavaPos.y + 1));

        for (int dx = -2; dx <= 2; dx++)
            TryIgniteAt(new Vector2Int(lavaPos.x + dx, lavaPos.y + 2));
    }

    private void TryIgniteAt(Vector2Int pos)
    {
        if (!map.CheckInsideBlockGrid(pos.x, pos.y)) return;
        if (map.FireManager.IsWetAt(pos)) return;
        Block blockAtPos = map.GetBlock(pos.x, pos.y);

        if (blockAtPos != null)
        {
            // Occupied cell: try side flame if the block itself is lava-igniteable.
            if (blockAtPos.GetComponent<FlammableObject>() is FlammableObject sideFlammable
                && sideFlammable.lavaIgnitability > 0
                && !sideFlammable.IsBurningAt(Vector2Int.zero))
            {
                float chance = sideFlammable.lavaIgnitability * map.FireManager.SpreadRateMultiplier / 300f;
                if (Random.value < chance)
                    map.FireManager.SetFire(sideFlammable, Vector2Int.zero, 0, immediateSchedule: true);
            }
        }
        else
        {
            // Empty cell: fire can appear here if any neighbor is lava-igniteable.
            int maxIgnitability = GetMaxNeighborLavaIgnitability(pos);
            if (maxIgnitability <= 0) return;
            if (map.FireManager.HasFlameAt(pos)) return; // cell already occupied

            // Try all supported directional flames for this empty cell, first valid support wins.
            if (TryIgniteDirectional(pos, Vector2Int.down, Vector2Int.up)) return;
            if (map.FireManager.AllowLeftRightFlames)
            {
                if (TryIgniteDirectional(pos, Vector2Int.right, Vector2Int.left)) return;
                if (TryIgniteDirectional(pos, Vector2Int.left,  Vector2Int.right)) return;
            }
            if (map.FireManager.AllowBottomFlames)
                TryIgniteDirectional(pos, Vector2Int.up, Vector2Int.down);
        }
    }

    // neighborDir: direction from pos toward the supporting block.
    // offset: direction from the supporting block toward pos (the inverse).
    // Returns true if a flame was placed.
    private bool TryIgniteDirectional(Vector2Int pos, Vector2Int neighborDir, Vector2Int offset)
    {
        Block b = map.GetBlock(pos.x + neighborDir.x, pos.y + neighborDir.y);
        if (b?.GetComponent<FlammableObject>() is FlammableObject f
            && f.IsFlammable
            && f.lavaIgnitability > 0
            && !f.IsBurningAt(offset))
        {
            float chance = f.lavaIgnitability * map.FireManager.SpreadRateMultiplier / 300f;
            if (Random.value < chance)
            {
                map.FireManager.SetFire(f, offset, 0, immediateSchedule: true);
                return true;
            }
        }
        return false;
    }

    private int GetMaxNeighborLavaIgnitability(Vector2Int pos)
    {
        int max = 0;
        foreach (var dir in CardinalDirs)
        {
            Block b = map.GetBlock(pos.x + dir.x, pos.y + dir.y);
            if (b?.GetComponent<FlammableObject>() is FlammableObject f)
                max = Mathf.Max(max, f.lavaIgnitability);
        }
        return max;
    }
}
