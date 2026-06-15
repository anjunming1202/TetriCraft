using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fire-spreading source component (e.g. on lava blocks).
/// Data: sourceStrength, spreadableArea.
/// Spreading logic runs here via random tick; Flame creation is delegated to FireManager.
/// </summary>
public class FlameSource : MapObject
{
    public float sourceStrength;
    public RectInt spreadableArea;
    public Vector2Int position => BoundaryDataManager.GetBoundaryData(map.PlayerID).WorldToGrid(transform.position);

    protected void Start()
    {
        MapRandomTickBehaviourObject mapObject = GetComponent<MapRandomTickBehaviourObject>();
        if (mapObject == null)
        {
            Debug.LogError($"[FlameSource] No MapRandomTickBehaviourObject on {gameObject.name} — FlameSource must be on a Block GameObject");
            return;
        }

        map = mapObject.GetMap();
        mapObject.OnRandomTickUpdate += RandomTickUpdate;
    }

    private void RandomTickUpdate(int randomTick)
    {
        if (randomTick % 1 == 0)
        {
            for (int i = 0; i < spreadAttempts; i++)
                TrySpreadFlame(randomTick, failedSpreadReattempts);
        }
    }

    private bool TrySpreadFlame(int randomTick, int reattempts)
    {
        for (int i = 0; i < reattempts; i++)
        {
            if (TrySpreadFlameOnce(randomTick))
                return true;
        }
        return false;
    }

    private bool TrySpreadFlameOnce(int randomTick)
    {
        int targetX = position.x + Random.Range(spreadableArea.xMin, spreadableArea.xMax + 1);
        int targetY = position.y + Random.Range(spreadableArea.yMin, spreadableArea.yMax + 1);

        if (targetX == position.x && targetY == position.y)
            return false;

        float distance = (new Vector2Int(targetX, targetY) - position).magnitude;
        List<FlammableObject> adjacentFlammableBlocks = GetAdjacentFlammableBlocksAll(targetX, targetY);

        bool hasSpread = false;

        foreach (FlammableObject target in adjacentFlammableBlocks)
        {
            if (!target.isFlammable)
                continue;

            Vector2Int flameOffset = target == GetAdjacentFlammableBlock(targetX, targetY, Vector2Int.down) ? Vector2Int.up : Vector2Int.zero;

            if (!target.IsBurningAt(flameOffset))
            {
                if (target.TryIgnite(distance, sourceStrength, adjacentFlammableBlocks))
                    if (!hasSpread && (map.GetBlock(targetX, targetY) == null || flameOffset == Vector2Int.zero))
                    {
                        map.FireManager.SetFire(target, flameOffset, randomTick);
                        hasSpread = true;
                    }
            }
            else
            {
                // probability reset fire
                if (randomTick % 1600 == 0)
                {
                    target.GetFlame(flameOffset).ResetFlame(randomTick);
                }
            }
        }
        return hasSpread;
    }

    private List<FlammableObject> GetAdjacentFlammableBlocksAll(int posX, int posY)
    {
        List<FlammableObject> adjacentFlammableBlocks = new List<FlammableObject>();

        foreach (var offset in new Vector2Int[] { Vector2Int.zero, Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up })
        {
            FlammableObject flammableBlock = GetAdjacentFlammableBlock(posX, posY, offset);
            if (flammableBlock != null)
                adjacentFlammableBlocks.Add(flammableBlock);
        }
        return adjacentFlammableBlocks;
    }

    private FlammableObject GetAdjacentFlammableBlock(int posX, int posY, Vector2Int offset)
    {
        int x = posX + offset.x;
        int y = posY + offset.y;
        Block adjacentBlock = map.GetBlock(x, y);
        if (adjacentBlock != null && adjacentBlock.GetComponent<FlammableObject>() is FlammableObject flammableBlock)
        {
            return flammableBlock;
        }
        return null;
    }

    private int spreadAttempts = 1;
    private int failedSpreadReattempts = 10;
}
