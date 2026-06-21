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
    /// <summary>
    /// The smoke child object whose world rotation should remain upright regardless of flame rotation.
    /// Assign the "Smoke" child transform in the inspector.
    /// </summary>
    [SerializeField] private Transform smoke;

    private bool isDead;

    private static readonly Vector2Int[] CardinalDirs =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    /// <param name="immediateSchedule">
    /// If true, skip the random-tick bootstrap and enter the self-scheduled cycle immediately.
    /// Use for spread-spawned flames; leave false for the first (manually lit) flame so it
    /// waits a natural random-tick delay before activating.
    /// </param>
    public void Init(MapManager map, FireManager fireManager, FlammableObject attachedFlammable, Vector2Int offset, int initialAge = 0, bool immediateSchedule = false)
    {
        this.map = map;
        this.fireManager = fireManager;
        this.attachedFlammable = attachedFlammable;
        this.offset = offset;

        attachedFlammable.SetBurningAt(offset, this);
        transform.parent = attachedFlammable.transform;
        transform.localPosition = (Vector2)offset;

        // Rotate topFlamePrefab-based flames to face away from their attached block.
        // Default prefab orientation is upward; left/right/bottom are rotated in code.
        if      (offset == Vector2Int.left)  transform.localRotation = Quaternion.Euler(0f, 0f,  90f);
        else if (offset == Vector2Int.right) transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        else if (offset == Vector2Int.down)  transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        if (smoke != null) smoke.rotation = Quaternion.identity;

        age = initialAge;
        isDead = false;

        attachedFlammable.GetComponent<Block>().OnAfterRemoved += OnAttachedBlockRemoved;

        if (immediateSchedule)
            ScheduleNextTick();
        else
            map.RandomTickManager.Register(this);
    }

    /// <summary>
    /// Unregister this flame from its FlammableObject and random tick system. Called by FireManager before destroying.
    /// </summary>
    public void DetachFromFlammable()
    {
        attachedFlammable.GetComponent<Block>().OnAfterRemoved -= OnAttachedBlockRemoved;
        attachedFlammable.StopBurningAt(offset);
        map.RandomTickManager.Unregister(this);
    }

    /// <summary>
    /// Safety cleanup for when this flame's GameObject is destroyed externally
    /// (e.g. parent block destroyed), bypassing the normal Die/Extinguish path.
    /// </summary>
    private void OnDestroy()
    {
        Block block = attachedFlammable?.GetComponent<Block>();
        if (block != null) block.OnAfterRemoved -= OnAttachedBlockRemoved;
        map?.RandomTickManager?.Unregister(this);
        fireManager?.UnregisterFlame(this);
    }

    /// <summary>
    /// Called when the attached block is removed or destroyed.
    /// Side flames die with their block; directional flames try to reparent to another support.
    /// </summary>
    private void OnAttachedBlockRemoved(Block _)
    {
        attachedFlammable.GetComponent<Block>().OnAfterRemoved -= OnAttachedBlockRemoved;
        if (isDead) return;

        // Side flames occupy the same cell as their block — when that block goes, there is
        // nothing left to support the flame in that cell, so let Unity destroy it as a child.
        if (offset == Vector2Int.zero) return;

        TryReparent();
    }

    /// <summary>
    /// After losing its attached block, try to reparent to another flammable block that
    /// borders the same flame cell. Mirrors Minecraft fire checking all 6 (here: 4) neighbors
    /// for any remaining solid face when its support is removed.
    /// </summary>
    private void TryReparent()
    {
        Vector2Int flameCell = position; // world-to-grid while still parented — still accurate

        // Detach before the block's GameObject is destroyed so we are not destroyed with it
        attachedFlammable.StopBurningAt(offset);
        transform.parent = null;

        // Try each directional support that could host the flame at flameCell (same priority order as spawn)
        if (TryAttachTo(flameCell, Vector2Int.down,  Vector2Int.up))    return;
        if (map.FireManager.AllowLeftRightFlames)
        {
            if (TryAttachTo(flameCell, Vector2Int.right, Vector2Int.left))  return;
            if (TryAttachTo(flameCell, Vector2Int.left,  Vector2Int.right)) return;
        }
        if (map.FireManager.AllowBottomFlames)
            if (TryAttachTo(flameCell, Vector2Int.up, Vector2Int.down)) return;

        isDead = true;
        fireManager.DestroyFlame(this);
    }

    // neighborOffset: direction from flameCell to the candidate support block.
    // newOffset:      direction from that block back to flameCell.
    // Returns true if reparenting succeeded.
    private bool TryAttachTo(Vector2Int flameCell, Vector2Int neighborOffset, Vector2Int newOffset)
    {
        Vector2Int neighborPos = flameCell + neighborOffset;
        var f = map.GetBlock(neighborPos.x, neighborPos.y)?.GetComponent<FlammableObject>();
        if (f == null || !f.IsFlammable || f.IsBurningAt(newOffset)) return false;

        attachedFlammable = f;
        offset = newOffset;
        f.SetBurningAt(offset, this);
        transform.parent = f.transform;
        transform.localPosition = (Vector2)offset;

        if      (offset == Vector2Int.left)  transform.localRotation = Quaternion.Euler(0f, 0f,  90f);
        else if (offset == Vector2Int.right) transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        else if (offset == Vector2Int.down)  transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        else                                 transform.localRotation = Quaternion.identity;

        // Keep smoke world-upright so its particles always rise vertically.
        if (smoke != null) smoke.rotation = Quaternion.identity;

        f.GetComponent<Block>().OnAfterRemoved += OnAttachedBlockRemoved;
        return true;
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
        if (self?.GetComponent<FlammableObject>() is FlammableObject sf && sf.IsFlammable)
            return true;

        foreach (var dir in CardinalDirs)
        {
            Block b = map.GetBlock(position.x + dir.x, position.y + dir.y);
            if (b?.GetComponent<FlammableObject>() is FlammableObject f && f.IsFlammable)
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
            float spreadChance = (maxEncouragement + 21f + 40f) / (age + 30f);
            if (UnityEngine.Random.value < spreadChance * fireManager.SpreadRateMultiplier / baseChance)
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
        if (self?.GetComponent<FlammableObject>() is FlammableObject sf && sf.IsFlammable)
            max = sf.encouragement;

        foreach (var dir in CardinalDirs)
        {
            Block b = map.GetBlock(pos.x + dir.x, pos.y + dir.y);
            if (b?.GetComponent<FlammableObject>() is FlammableObject f && f.IsFlammable)
                max = Mathf.Max(max, f.encouragement);
        }
        return max;
    }

    // Place fire: side flame on block at spreadPos, or directional flames on any adjacent block
    private void TryPlaceFireAt(Vector2Int spreadPos, int randomTick)
    {
        if (!map.CheckInsideBlockGrid(spreadPos.x, spreadPos.y)) return;
        Block blockAtPos = map.GetBlock(spreadPos.x, spreadPos.y);

        // Side flame: flammable block occupies spreadPos
        if (blockAtPos?.GetComponent<FlammableObject>() is FlammableObject sideFlammable
            && sideFlammable.IsFlammable
            && !sideFlammable.IsBurningAt(Vector2Int.zero))
        {
            map.FireManager.SetFire(sideFlammable, Vector2Int.zero, randomTick, immediateSchedule: true);
            return;
        }

        // Directional flames: empty cell — one flame per cell maximum, first valid support wins
        if (blockAtPos != null) return;
        if (map.FireManager.HasFlameAt(spreadPos)) return;
        if (TryPlaceDirectionalFlame(spreadPos, Vector2Int.down,  Vector2Int.up,    randomTick)) return;
        if (map.FireManager.AllowLeftRightFlames)
        {
            if (TryPlaceDirectionalFlame(spreadPos, Vector2Int.right, Vector2Int.left,  randomTick)) return;
            if (TryPlaceDirectionalFlame(spreadPos, Vector2Int.left,  Vector2Int.right, randomTick)) return;
        }
        if (map.FireManager.AllowBottomFlames)
            TryPlaceDirectionalFlame(spreadPos, Vector2Int.up, Vector2Int.down, randomTick);
    }

    // neighborDir: direction from spreadPos toward the supporting block.
    // offset: direction from the supporting block toward spreadPos (the inverse).
    // Returns true if a flame was placed.
    private bool TryPlaceDirectionalFlame(Vector2Int spreadPos, Vector2Int neighborDir, Vector2Int offset, int randomTick)
    {
        Vector2Int neighborPos = spreadPos + neighborDir;
        Block b = map.GetBlock(neighborPos.x, neighborPos.y);
        if (b?.GetComponent<FlammableObject>() is FlammableObject f
            && f.IsFlammable
            && !f.IsBurningAt(offset))
        {
            map.FireManager.SetFire(f, offset, randomTick, immediateSchedule: true);
            return true;
        }
        return false;
    }

    // Side flame: only burns its own attached block (dir zero, divisor 300).
    // Top flame: burns the four cardinal neighbors of its position (up/down use 250, left/right use 300).
    private void TryBurnAdjacentBlocks()
    {
        Vector2Int[] checkDirs = offset == Vector2Int.zero
            ? new[] { Vector2Int.zero }
            : new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in checkDirs)
        {
            Block b = map.GetBlock(position.x + dir.x, position.y + dir.y);
            if (b?.GetComponent<FlammableObject>() is not FlammableObject f) continue;
            if (!f.CanBurnAway) continue;

            float divisor = (dir == Vector2Int.up || dir == Vector2Int.down) ? 250f : 300f;
            if (UnityEngine.Random.value < f.flammability * fireManager.BurnRateMultiplier / divisor)
            {
                f.BurnAway();
                TrySpawnFireAfterBurn(b, position + dir);
            }
        }
    }

    // Wiki: after a block burns, 5/(10+age) chance to spawn fire in its place.
    // New flame age: 80% same as this flame, 20% this flame's age +1.
    // Subscribe to OnAfterRemoved so the spawn runs after the grid slot is vacated —
    // this avoids attaching a flame to a block that is being destroyed/removed.
    // The lambda intentionally does not capture 'this'; it only needs the already-computed
    // position, age, and map reference, so it is safe even if this Flame is destroyed first.
    private void TrySpawnFireAfterBurn(Block burnedBlock, Vector2Int burnedPos)
    {
        if (UnityEngine.Random.value >= 5f / (10 + age)) return;

        int newAge = UnityEngine.Random.value < 0.8f ? age : Mathf.Min(age + 1, maxAge);
        MapManager capturedMap = map;

        void OnRemoved(Block _)
        {
            burnedBlock.OnAfterRemoved -= OnRemoved;

            // Position is now empty — try a top flame on the block below.
            if (!capturedMap.FireManager.HasFlameAt(burnedPos))
            {
                Block blockBelow = capturedMap.GetBlock(burnedPos.x, burnedPos.y - 1);
                if (blockBelow?.GetComponent<FlammableObject>() is FlammableObject topFlammable
                    && topFlammable.IsFlammable
                    && !topFlammable.IsBurningAt(Vector2Int.up))
                {
                    capturedMap.FireManager.SetFire(topFlammable, Vector2Int.up, 0, newAge, immediateSchedule: true);
                }
            }
        }

        burnedBlock.OnAfterRemoved += OnRemoved;
    }
}
