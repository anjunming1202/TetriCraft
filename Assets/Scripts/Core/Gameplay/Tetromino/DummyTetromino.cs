using System;
using UnityEngine;

public class DummyTetromino : Tetromino
{ 
    public void Display()
    {
        int blockCount = 0;
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                Block block = shape[r, c];
                if (block == null)
                    continue;
                block.transform.position = transform.position + MapBoundaryData.MapToWorld(new Vector2(c + 0.5f - (float)size / 2, -r + 0.5f + (float)size / 2));
                blockCount++;
            }

        // set unused ghost block invisible
        for (int i = blockCount; i < 4; i++)
            blocks[i].transform.position = new Vector3(-1, -1, 0); // somewhere hidden
    }

    public void SetPosition(Vector2Int mapPosition)
    {
        position = mapPosition;
        transform.position = GetWorldPosition();
        Display();
    }

    public void Transform(Tetromino tetrominoShape)
    {
        //Vector2Int position = this.position;
        //this.position = position;

        type = tetrominoShape.type;
        size = tetrominoShape.size;
        shape = new Block[size, size];
        int blockCount = 0;
        for (int r = 0; r < tetrominoShape.size; r++)
            for (int c = 0; c < tetrominoShape.size; c++)
            {
                if (tetrominoShape.shape[r, c] != null)
                {
                    Block block = blocks[blockCount];
                    shape[r, c] = block;
                    block.OnTriggerAppearanceChanged();
                    blockCount++;
                }
            }
    }
}

public class GhostTetromino : DummyTetromino
{
    public Tetromino shadowTetromino;
}
