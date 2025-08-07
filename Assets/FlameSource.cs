using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameSource : MonoBehaviour
{
    public Flame flamePrefab;
    public RectInt spreadableArea;
    public Vector2Int position => MapBoundaryData.WorldToGrid(transform.position);

    private void Awake()
    {
        map = GetComponent<MapObject>().GetMap();  
    }

    private void Update()
    {
        
    }

    private void TrySpreadFlame()
    {
        int targetX = position.x + Random.Range(spreadableArea.xMin, spreadableArea.xMax + 1);
        int targetY = position.y + Random.Range(spreadableArea.yMin, spreadableArea.yMax + 1);

        foreach (var offset in new Vector2Int[] { Vector2Int.zero, Vector2Int.down })
        {
            int x = targetX + offset.x;
            int y = targetY + offset.y;
            Block attacthedBlock = map.GetBlock(x, y);
        }
    }

    private MapManager map;
}
