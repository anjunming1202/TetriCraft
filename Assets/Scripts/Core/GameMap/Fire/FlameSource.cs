using UnityEngine;

/// <summary>
/// Fire-spreading source component (e.g. on lava blocks).
/// Ignites adjacent flammable blocks on a fixed game-tick interval via ScheduledTickManager,
/// mirroring Minecraft lava's scheduled-tick behavior (~30 ticks = 1.5 s at 20 Hz).
/// </summary>
public class FlameSource : MapObject
{
    public float sourceStrength = 1f;

    /// <summary>How many game ticks between ignition checks. Default 30 = 1.5 s, matching Minecraft lava tick rate.</summary>
    [SerializeField] private int spreadInterval = 30;

    public Vector2Int position => BoundaryDataManager.GetBoundaryData(map.PlayerID).WorldToGrid(transform.position);

    private bool isDead;

    private static readonly Vector2Int[] CardinalDirs =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private void Start()
    {
        map = GetComponent<Block>().GetMap();
        // Stagger initial tick so multiple lava sources don't all fire on the same tick.
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
        foreach (var dir in CardinalDirs)
        {
            int nx = position.x + dir.x;
            int ny = position.y + dir.y;
            Block neighbor = map.GetBlock(nx, ny);
            if (neighbor?.GetComponent<FlammableObject>() is not FlammableObject f || !f.isFlammable)
                continue;
            if (f.IsBurningAt(Vector2Int.zero)) continue;

            // Lava ignition probability: uses encouragement + sourceStrength multiplier
            float igniteChance = (f.encouragement + 40f) * sourceStrength / 30f;
            if (Random.value < igniteChance / 300f)
                map.FireManager.SetFire(f, Vector2Int.zero, 0);
        }
    }
}
