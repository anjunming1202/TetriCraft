using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameSource : MonoBehaviour
{
    public Flame sideFlamePrefab;
    public Flame topFlamePrefab;
    public RectInt spreadableArea;
    public Vector2Int position => MapBoundaryData.WorldToGrid(transform.position);

    private void Start()
    {
        map = GetComponent<MapObject>().GetMap();  
    }

    private void Update()
    {
        TrySpreadFlame();
    }

    private void TrySpreadFlame()
    {
        int targetX = position.x + Random.Range(spreadableArea.xMin, spreadableArea.xMax + 1);
        int targetY = position.y + Random.Range(spreadableArea.yMin, spreadableArea.yMax + 1);

        foreach (var offset in new Vector2Int[] { Vector2Int.zero, Vector2Int.down })
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

                    if (!target.IsBurningAt(-offset))
                        SetFire(target, -offset);
                    else
                        target.GetFlame(-offset).Reset();
                }                
                return;
            }
        }
    }

    private void SetFire(FlammableObject attachedTarget, Vector2Int offset)
    {
        Flame flame = Instantiate(offset == Vector2Int.zero ? sideFlamePrefab : topFlamePrefab);
        flame.Init(map, attachedTarget, offset);
    }

    private MapManager map;
}
