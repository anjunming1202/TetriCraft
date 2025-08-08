using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameSource : MapObject
{
    public Flame sideFlamePrefab;
    public Flame topFlamePrefab;
    public float strength;
    public RectInt spreadableArea;
    public Vector2Int position => MapBoundaryData.WorldToGrid(transform.position);

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
                TrySpreadFlame(randomTick);
        }
    }

    private void TrySpreadFlame(int randomTick)
    {
        int targetX = position.x + Random.Range(spreadableArea.xMin, spreadableArea.xMax + 1);
        int targetY = position.y + Random.Range(spreadableArea.yMin, spreadableArea.yMax + 1);
        /*if (targetX == spreadableArea.xMin || targetX == spreadableArea.xMax)
            targetY = position.y + Random.Range(spreadableArea.yMin + 1, spreadableArea.yMax);
        else
            targetY = position.y + Random.Range(spreadableArea.yMin, spreadableArea.yMax + 1);*/


        foreach (var offset in new Vector2Int[] { Vector2Int.zero, Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up })
        {
            int x = targetX + offset.x;
            int y = targetY + offset.y;
            Block attachedBlock = map.GetBlock(x, y);
            if (attachedBlock != null)
            {
                if (attachedBlock.GetComponent<FlammableObject>() is FlammableObject target)
                {
                    if (!target.isFlammable)
                        return;

                    Vector2Int flamePosition = offset == Vector2Int.down ? -offset : Vector2Int.zero;

                    if (!target.IsBurningAt(flamePosition))
                    {
                        float distance = (new Vector2Int(x, y) - position).magnitude;
                        if (target.TryIgnite(distance, strength))
                        {
                            SetFire(target, flamePosition);
                            return;
                        }
                    }
                    else
                        target.GetFlame(flamePosition).Reset();
                }
                return;
            }
        }
    }

    private void SetFire(FlammableObject attachedTarget, Vector2Int offset)
    {
        Flame flame = Instantiate(offset == Vector2Int.zero ? sideFlamePrefab : topFlamePrefab);
        flame.Init(map, attachedTarget, offset);
        // burn once when set
        flame.Burn();
    }

    private int spreadAttempts = 3;
}
