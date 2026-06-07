using System;
using UnityEngine;

public class DummyTetromino : Tetromino
{ 
    public void Display(MapManager map)
    {
        // set tetromino (parent to blocks) position
        transform.position = BoundaryDataManager.GetBoundaryData(map.PlayerID).GridToWorld(position);

        // set blocks position
        int blockCount = 0;
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                Block block = shape[r, c];
                if (block == null)
                    continue;
                block.transform.position = transform.position + MapBoundaryData.MapToWorldRelative(LocalToMap(r, c) - position);
                blockCount++;
            }

        // set unused ghost block invisible
        for (int i = blockCount; i < 4; i++)
            blocks[i].transform.position = new Vector3(-1, -1, 0); // somewhere hidden
    }

    public void SetPosition(Vector2Int mapPosition, MapManager map)
    {
        position = mapPosition;
        Display(map);
    }
}
