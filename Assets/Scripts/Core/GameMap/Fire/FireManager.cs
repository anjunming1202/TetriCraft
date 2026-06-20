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
    /// offset == zero means side flame (inner block); offset == up means top flame (on top of block).
    /// </summary>
    public void SetFire(FlammableObject attachedBlock, Vector2Int offset, int randomTick, int initialAge = 0)
    {
        Flame flame = Instantiate(offset == Vector2Int.zero ? sideFlamePrefab : topFlamePrefab);
        flame.Init(map, this, attachedBlock, offset, initialAge);
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
    /// Try to extinguish flames that overlap with a newly placed block (e.g. water flowing in).
    /// Formerly static Flame.TryExtinguishBy().
    /// </summary>
    public void TryExtinguishAt(Block block)
    {
        Vector2Int position = block.GridPosition;

        if (block is WaterDummy waterDummy)
        {
            FluidElement element = waterDummy.GetSourceElement();
            // down
            if (element.lowerGridPosition == position.y && element.localLowerLevel == 0)
                TryExtinguishInnerFlameAt(position + Vector2Int.down);
            // up
            if (element.upperGridPosition == position.y && element.localUpperLevel == 0)
                TryExtinguishInnerFlameAt(position + Vector2Int.up);
            // sides
            TryExtinguishInnerFlameAt(position + Vector2Int.left);
            TryExtinguishInnerFlameAt(position + Vector2Int.right);
        }

        // extinguish top flame on the block directly below
        Block blockBelow = map.GetBlock(position.x, position.y - 1);
        if (blockBelow != null && blockBelow.GetComponent<FlammableObject>() is FlammableObject flammableObject)
        {
            Flame flame = flammableObject.GetFlame(Vector2Int.up);
            if (flame != null)
            {
                if (block is WaterDummy)
                    flame.Extinguish();
                else
                    flame.Die();
            }
        }
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
