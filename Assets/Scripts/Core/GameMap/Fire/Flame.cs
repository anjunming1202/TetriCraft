using System;
using UnityEngine;

/// <summary>
/// A single fire instance. Handles aging, spreading, and block destruction via Minecraft-style probability.
/// Lifecycle (spawn/destroy) is owned by FireManager — Flame never destroys itself directly.
///
/// Tick model: random tick fires once to bootstrap the flame, then the flame self-schedules
/// via ScheduledTickManager every 30–39 ticks — independent of pool competition.
/// </summary>
public class Flame : MapObject, IRandomTickable
{
    public event Action<int> OnRandomTickUpdate;

    public Vector2Int position => BoundaryDataManager.GetBoundaryData(map.PlayerID).WorldToGrid(transform.position);
    public int age;

    private int maxAge = 15;
    private FlammableObject attachedFlammable;
    private Vector2Int offset;
    private FireManager fireManager;

    [SerializeField] AudioClip extinguishSound;
    [SerializeField] private int minTickDelay = 30;
    [SerializeField] private int maxTickDelay = 40;

    private bool isDead;

    private static readonly Vector2Int[] CardinalDirs =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    public void Init(MapManager map, FireManager fireManager, FlammableObject attachedFlammable, Vector2Int offset)
    {
        this.map = map;
        this.fireManager = fireManager;
        this.attachedFlammable = attachedFlammable;
        this.offset = offset;

        attachedFlammable.SetBurningAt(offset, this);
        transform.parent = attachedFlammable.transform;
        transform.localPosition = (Vector2)offset;

        age = 0;
        isDead = false;

        map.RandomTickManager.Register(this);
    }

    /// <summary>
    /// Unregister this flame from its FlammableObject and random tick system. Called by FireManager before destroying.
    /// </summary>
    public void DetachFromFlammable()
    {
        attachedFlammable.StopBurningAt(offset);
        map.RandomTickManager.Unregister(this);
    }

    /// <summary>
    /// Safety cleanup for when this flame's GameObject is destroyed externally
    /// (e.g. parent block destroyed), bypassing the normal Die/Extinguish path.
    /// </summary>
    private void OnDestroy()
    {
        map?.RandomTickManager?.Unregister(this);
        fireManager?.UnregisterFlame(this);
    }

    /// <summary>
    /// Called once by the random tick pool to bootstrap this flame.
    /// Leaves the pool immediately and transitions to self-scheduled ticks.
    /// </summary>
    public void RandomTickUpdate(int randomTick)
    {
        map.RandomTickManager.Unregister(this); // leave pool — no more pool competition
        PerformTick(randomTick);
        if (!isDead) ScheduleNextTick();
    }

    /// <summary>
    /// Extinguish with sound effect (e.g. by water). Delegates destruction to FireManager.
    /// </summary>
    public void Extinguish()
    {
        isDead = true;
        AudioManager.Instance.PlaySFXAtPoint(extinguishSound, transform.position, 1f, AudioBus.Block);
        fireManager.DestroyFlame(this);
    }

    /// <summary>
    /// Silent death (aged out or manually removed). Delegates destruction to FireManager.
    /// </summary>
    public void Die()
    {
        isDead = true;
        fireManager.DestroyFlame(this);
    }

    private void ScheduleNextTick()
    {
        map.ScheduledTickManager.Schedule(OnScheduledTick,
            UnityEngine.Random.Range(minTickDelay, maxTickDelay),
            transform.position);
    }

    private void OnScheduledTick()
    {
        if (isDead || this == null) return; // destroyed externally or via normal path
        PerformTick(UnityEngine.Random.Range(0, int.MaxValue));
        if (!isDead) ScheduleNextTick();
    }

    private void PerformTick(int randomTick)
    {
        TryAgeGrow();
        TryBurnout();        // may call Die() → sets isDead = true
        if (isDead) return;
        TrySpreadFire(randomTick);
        TryBurnAdjacentBlocks();
        OnRandomTickUpdate?.Invoke(randomTick);
    }

    // Stochastic age increment — ~1/3 chance per tick (mirrors Minecraft behavior)
    private void TryAgeGrow()
    {
        if (UnityEngine.Random.Range(0, 3) == 0)
            age = Mathf.Min(maxAge, age + 1);
    }

    // At max age, die unless sustained by a flammable neighbor
    private void TryBurnout()
    {
        if (age < maxAge) return;
        if (!HasFlammableNeighbor() || UnityEngine.Random.Range(0, 4) == 0)
            Die();
    }

    private bool HasFlammableNeighbor()
    {
        // Check self position first: side flames sit at the same grid cell as their attached block,
        // so the attached block won't appear in any cardinal direction.
        Block self = map.GetBlock(position.x, position.y);
        if (self?.GetComponent<FlammableObject>() is FlammableObject sf && sf.isFlammable)
            return true;

        foreach (var dir in CardinalDirs)
        {
            Block b = map.GetBlock(position.x + dir.x, position.y + dir.y);
            if (b?.GetComponent<FlammableObject>() is FlammableObject f && f.isFlammable)
                return true;
        }
        return false;
    }

    // Minecraft spread formula — 2D range: dx[-1..1], dy[-1..4]
    // baseChance mirrors Minecraft height scaling: 100 within dy<=1, +100 per block above that.
    private void TrySpreadFire(int randomTick)
    {
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 4; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int baseChance = dy > 1 ? 100 + (dy - 1) * 100 : 100;
            Vector2Int spreadPos = new Vector2Int(position.x + dx, position.y + dy);
            int maxEncouragement = GetMaxNeighborEncouragement(spreadPos);
            if (maxEncouragement <= 0) continue;
            float spreadChance = (maxEncouragement + 40f) / (age + 30f);
            if (UnityEngine.Random.value < spreadChance / (baseChance * fireManager.SpreadRateMultiplier))
                TryPlaceFireAt(spreadPos, randomTick);
        }
    }

    // Returns highest encouragement among pos itself and its 4 cardinal neighbors.
    // Self is included because a side flame sits AT the flammable block's position —
    // that block's own encouragement must count toward spread probability.
    // For top flames pos is empty air, so self contributes 0 (no effect).
    private int GetMaxNeighborEncouragement(Vector2Int pos)
    {
        int max = 0;
        Block self = map.GetBlock(pos.x, pos.y);
        if (self?.GetComponent<FlammableObject>() is FlammableObject sf && sf.isFlammable)
            max = sf.encouragement;

        foreach (var dir in CardinalDirs)
        {
            Block b = map.GetBlock(pos.x + dir.x, pos.y + dir.y);
            if (b?.GetComponent<FlammableObject>() is FlammableObject f && f.isFlammable)
                max = Mathf.Max(max, f.encouragement);
        }
        return max;
    }

    // Place fire: side flame on block at spreadPos, or top flame on block below empty spreadPos
    private void TryPlaceFireAt(Vector2Int spreadPos, int randomTick)
    {
        Block blockAtPos = map.GetBlock(spreadPos.x, spreadPos.y);

        // Side flame: flammable block occupies spreadPos
        if (blockAtPos?.GetComponent<FlammableObject>() is FlammableObject sideFlammable
            && sideFlammable.isFlammable
            && !sideFlammable.IsBurningAt(Vector2Int.zero))
        {
            map.FireManager.SetFire(sideFlammable, Vector2Int.zero, randomTick);
            return;
        }

        // Top flame: empty cell, flammable block directly below
        if (blockAtPos == null)
        {
            Block blockBelow = map.GetBlock(spreadPos.x, spreadPos.y - 1);
            if (blockBelow?.GetComponent<FlammableObject>() is FlammableObject topFlammable
                && topFlammable.isFlammable
                && !topFlammable.IsBurningAt(Vector2Int.up))
            {
                map.FireManager.SetFire(topFlammable, Vector2Int.up, randomTick);
            }
        }
    }

    // Minecraft burn formula — immediate destruction for attached block + 4 cardinal neighbors
    private void TryBurnAdjacentBlocks()
    {
        Vector2Int[] checkDirs = { Vector2Int.zero, Vector2Int.up, Vector2Int.down,
                                    Vector2Int.left, Vector2Int.right };
        foreach (var dir in checkDirs)
        {
            Block b = map.GetBlock(position.x + dir.x, position.y + dir.y);
            if (b?.GetComponent<FlammableObject>() is not FlammableObject f) continue;
            if (!f.isFlammable || f.flammability <= 0) continue;
            float burnChance = (f.flammability + 10f) / (age + 30f);
            if (UnityEngine.Random.value < burnChance / (300f * fireManager.BurnRateMultiplier))
                f.BurnAway();
        }
    }
}
