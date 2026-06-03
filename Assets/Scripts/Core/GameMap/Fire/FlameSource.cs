using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameSource : MapObject
{
    public Flame sideFlamePrefab;
    public Flame topFlamePrefab;
    public float sourceStrength;
    public RectInt spreadableArea;
    public Vector2Int position => BoundaryDataManager.GetBoundaryData(map.PlayerID).WorldToGrid(transform.position);

    protected void Start()
    {
        MapRandomTickBehaviourObject mapObject = GetComponent<MapRandomTickBehaviourObject>();
        map = mapObject.GetMap();

        mapObject.OnRandomTickUpdate += RandomTickUpdateSpread;
    }

    /*private void Update()
    {
        TrySpreadFlame();
    }*/

    private void RandomTickUpdateSpread(int randomTick)
    {
        Debug.Log("try spread fire");

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
            if (TrySpreadFlame(randomTick))
                return true;
        }
        return false;
    }

    private bool TrySpreadFlame(int randomTick)
    {
        int targetX = position.x + Random.Range(spreadableArea.xMin, spreadableArea.xMax + 1);
        int targetY = position.y + Random.Range(spreadableArea.yMin, spreadableArea.yMax + 1);
        /*if (targetX == spreadableArea.xMin || targetX == spreadableArea.xMax)
            targetY = position.y + Random.Range(spreadableArea.yMin + 1, spreadableArea.yMax);
        else
            targetY = position.y + Random.Range(spreadableArea.yMin, spreadableArea.yMax + 1);*/

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
                // a position able to set fire => try to ignite, depends on adjacent flammability
                if (target.TryIgnite(distance, sourceStrength, adjacentFlammableBlocks))
                    if (!hasSpread && (map.GetBlock(targetX, targetY) == null || flameOffset == Vector2Int.zero))
                    {
                        SetFire(target, flameOffset, randomTick);
                        hasSpread = true;
                    }
            }
            else
            {
                // ... probability reset fire (reset flame)
                if (randomTick % (1600) == 0)
                {
                    target.GetFlame(flameOffset).ResetFlame(randomTick);
                }
            }
        }
        return hasSpread;
    }

    private void SetFire(FlammableObject attachedBlock, Vector2Int offset, int randomTick)
    {
        Flame flame = Instantiate(offset == Vector2Int.zero ? sideFlamePrefab : topFlamePrefab);
        flame.Init(map, attachedBlock, offset);
        // burn once when set
        flame.Burn(randomTick);
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
