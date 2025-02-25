using System.Drawing;
using Unity.Mathematics;
using UnityEngine;

public class Map
{
    public Map()
    {
        blockMap = new Block[width, height + 5]; // all null
    }

    public Block[,] blockMap;

    // Map Boundary Data
    private int width => MapBoundaryData.Instance.width;
    private int height => MapBoundaryData.Instance.height;
    public Block this[int x, int y]
    {
        get => blockMap[x, y];
        set => blockMap[x, y] = value;
    }

    /// <summary>
    /// Set position of tetromino, position of blocks contained, and update block map.
    /// </summary>
    public void SetTetrominoPosition(Tetromino tetromino, Vector2Int position)
    {
        // Set tetromino data -> set position
        tetromino.position = position;
        // Set block data & update map data
        SetBlocksPosition(tetromino);
    }
    /// <summary>
    /// Set position of blocks contained and update block map, based on current tetromino data
    /// </summary>
    public void SetBlocksPosition(Tetromino tetromino)
    {
        // Getting blocks reference -> set block position by tetromino position + local position in the tetromino
        for (int c = 0; c < tetromino.size; c++)
            for (int r = 0; r < tetromino.size; r++)
            {
                Block block = tetromino[r, c];
                if (block != null)
                {
                    Vector2Int to = tetromino.LocalToMap(r, c);
                    Vector2Int from = block.position;
                    // Set block data
                    block.position = to;
                    // Set map data
                    blockMap[from.x, from.y] = null;
                    blockMap[to.x, to.y] = block;
                }
            }
    }

    /// <summary>
    /// Check for bottom, left, and right boundaries
    /// </summary>
    public bool CheckInside(Tetromino tetromino)
    {
        for (int r = 0; r < tetromino.size; r++)
            for (int c = 0; c < tetromino.size; c++)
            {
                if (tetromino[r, c] != null)
                {
                    Vector2Int blockPos = tetromino.LocalToMap(r, c);
                    if (!CheckInside(blockPos.x, blockPos.y))
                        return false;
                }
            }
        return true;
    }
    /// <summary>
    /// Check for bottom, left, and right boundaries
    /// </summary>
    public bool CheckInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0;
    }
    /*
    public void AddBlock(Block block)
    {
        blockMap[block.position.x, block.position.y] = block;
    }
    private void MoveGrid(Vector2Int from, Vector2Int to)
    {
        blockMap[to.x, to.y] = blockMap[from.x, from.y];
        blockMap[from.x, from.y] = null;
    }*/
}
