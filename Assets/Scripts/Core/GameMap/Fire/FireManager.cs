using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central coordinator for all fire behavior.
/// Owns: active Flame registry, Flame prefabs, SetFire factory, extinguish logic, Clear.
/// </summary>
public class FireManager : MonoBehaviour
{
    [SerializeField] private Flame sideFlamePrefab;
    [SerializeField] private Flame topFlamePrefab;

    [Header("Rate Multipliers")]
    [SerializeField] private float spreadRateMultiplier = 1f;
    [SerializeField] private float burnRateMultiplier   = 1f;

    public float SpreadRateMultiplier => spreadRateMultiplier;
    public float BurnRateMultiplier   => burnRateMultiplier;

    [Header("Allowed Flame Types")]
    [SerializeField] private bool allowLeftRightFlames = true;
    [SerializeField] private bool allowBottomFlames    = false;

    public bool AllowLeftRightFlames => allowLeftRightFlames;
    public bool AllowBottomFlames    => allowBottomFlames;

    private MapManager map;
    private readonly List<Flame> flames = new();

    public void Init(MapManager map)
    {
        this.map = map;
    }

    public void Clear()
    {
        for (int i = flames.Count - 1; i >= 0; i--)
        {
            if (flames[i] != null)
                GameObject.Destroy(flames[i].gameObject);
        }
        flames.Clear();
    }

    /// <summary>
    /// Instantiate and register a new Flame on <paramref name="attachedBlock"/> at <paramref name="offset"/>.
    /// offset == zero means side flame (inner block); any other offset means directional flame.
    /// Pass immediateSchedule = true for spread-spawned flames to skip the random-tick bootstrap.
    /// </summary>
    public void SetFire(FlammableObject attachedBlock, Vector2Int offset, int randomTick, int initialAge = 0, bool immediateSchedule = false)
    {
        Flame flame = Instantiate(offset == Vector2Int.zero ? sideFlamePrefab : topFlamePrefab);
        flame.Init(map, this, attachedBlock, offset, initialAge, immediateSchedule);
        flames.Add(flame);
    }

    /// <summary>
    /// Destroy a flame: detach it from its FlammableObject, remove from registry, destroy GameObject.
    /// Called by Flame.Die() and Flame.Extinguish().
    /// </summary>
    public void DestroyFlame(Flame flame)
    {
        flame.DetachFromFlammable();
        flames.Remove(flame);
        GameObject.Destroy(flame.gameObject);
    }

    /// <summary>
    /// Remove flame from registry without destroying its GameObject.
    /// Called by Flame.OnDestroy when the GameObject is destroyed externally.
    /// </summary>
    public void UnregisterFlame(Flame flame)
    {
        flames.Remove(flame);
    }

    /// <summary>
    /// Returns true if any active flame currently occupies the given grid cell.
    /// Used to prevent multiple directional flames from spawning in the same empty cell.
    /// </summary>
    public bool HasFlameAt(Vector2Int gridPos)
    {
        foreach (var flame in flames)
            if (flame != null && flame.position == gridPos)
                return true;
        return false;
    }

    /// <summary>
    /// Try to extinguish flames that overlap with a newly placed block (e.g. water flowing in).
    /// Covers all 5 flame types: side (zero), top (up), left, right, bottom.
    /// </summary>
    public void TryExtinguishAt(Block block)
    {
        Vector2Int p = block.GridPosition;
        bool isWater = block is WaterDummy;

        if (isWater)
        {
            WaterDummy waterDummy = (WaterDummy)block;
            FluidElement element = waterDummy.GetSourceElement();
            // down
            if (element.lowerGridPosition == p.y && element.localLowerLevel == 0)
                TryExtinguishInnerFlameAt(p + Vector2Int.down);
            // up
            if (element.upperGridPosition == p.y && element.localUpperLevel == 0)
                TryExtinguishInnerFlameAt(p + Vector2Int.up);
            // sides
            TryExtinguishInnerFlameAt(p + Vector2Int.left);
            TryExtinguishInnerFlameAt(p + Vector2Int.right);
        }

        // Extinguish any directional flame whose flame cell equals p
        TryExtinguishFlame(p.x,   p.y,   Vector2Int.zero,  isWater); // side flame on block at p
        TryExtinguishFlame(p.x,   p.y-1, Vector2Int.up,    isWater); // top flame on block below p
        TryExtinguishFlame(p.x+1, p.y,   Vector2Int.left,  isWater); // left flame on block right of p
        TryExtinguishFlame(p.x-1, p.y,   Vector2Int.right, isWater); // right flame on block left of p
        TryExtinguishFlame(p.x,   p.y+1, Vector2Int.down,  isWater); // bottom flame on block above p
    }

    private void TryExtinguishFlame(int bx, int by, Vector2Int offset, bool isWater)
    {
        var f = map.GetBlock(bx, by)?.GetComponent<FlammableObject>();
        if (f == null) return;
        Flame flame = f.GetFlame(offset);
        if (flame == null) return;
        if (isWater) flame.Extinguish(); else flame.Die();
    }

    private void TryExtinguishInnerFlameAt(Vector2Int position)
    {
        Block blockTarget = map.GetBlock(position.x, position.y);
        if (blockTarget != null && blockTarget.GetComponent<FlammableObject>() is FlammableObject flammableObject)
        {
            Flame flame = flammableObject.GetFlame(Vector2Int.zero);
            if (flame != null)
                flame.Extinguish();
        }
    }
}
