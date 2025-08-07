using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flame : MapObject
{
    public Vector2Int position => MapBoundaryData.WorldToGrid(transform.position);
    public int age;

    public void Init(MapManager map, int x, int y)
    {
        this.map = map;
        transform.position = MapBoundaryData.GridToWorld(new Vector2Int(x, y));
        age = 0;
    }

    private Block attachedBlock;
}
