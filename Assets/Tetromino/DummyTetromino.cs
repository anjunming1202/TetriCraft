using System;
using UnityEngine;

public class DummyTetromino : Tetromino
{ 
    public void Display()
    {
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                Block block = shape[r, c];
                if (block == null)
                    continue;
                block.transform.position = transform.position + MapBoundaryData.GridToWorld(new Vector2(c - (float)size / 2, -r + (float)size / 2));
            }
    }

    public void SetPosition(Vector2Int mapPosition)
    {
        position = mapPosition;
        transform.position = GetWorldPosition();
        Display();
    }

    public void Transform(Tetromino tetrominoShape)
    {
        Vector2Int position = this.position;
        New(tetrominoShape.type, blocks[0], blocks[1], blocks[2], blocks[3]);
        this.position = position;

        for (int i = 0; i < tetrominoShape.rotation; i++)
        {
            RotateShape(false);
        }
    }
}
