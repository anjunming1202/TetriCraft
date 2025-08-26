using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BlockGrid
{
    public BlockGrid(int width, int height)
    {
        this.width = width;
        this.height = height;
        grid = new Block[width, height + 5];
        positions = new Dictionary<Block, Vector2Int>();
    }

    public Block this[int x, int y] => grid[x, y];
    public int blockCount => positions.Count;   // debug

    public void Add(Block block)
    {
        int x = block.GridPosition.x;
        int y = block.GridPosition.y;

        Debug.Assert(grid[x, y] == null, "Block position overlap!");
        grid[x, y] = block;

        positions.Add(block, block.GridPosition);

        block.isInMap = true;

        // grid block update
        OnBlockUpdate(block.GridPosition);
    }

    public void Remove(Block block)
    {
        Debug.Assert(grid[positions[block].x, positions[block].y] == block, "Block position inconsistent!");
        grid[positions[block].x, positions[block].y] = null;

        positions.Remove(block);

        block.isInMap = false;

        // grid block update
        OnBlockUpdate(block.GridPosition);
    }

    public Block Get(int x, int y)
    {
        if (CheckInsideGrid(x, y))
            return grid[x, y];
        return null;
    }

    public bool Contains(Block block)
    {
        foreach (Block gridBlock in grid)
        {
            if (gridBlock == block) 
                return true;
        }
        return false;
    }

    public bool CheckInsideGrid(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool CheckInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0;
    }

    public bool CheckEmpty(int x, int y)
    {
        if (y >= height)
            return true;

        return grid[x, y] == null;
    }

    public void OnBlockUpdate(Vector2Int pos)
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            Vector2Int nPos = pos + dir;
            Block neighbour = Get(nPos.x, nPos.y);
            if (neighbour != null)
            {
                neighbour.OnNeighbourUpdated();
            }
        }
    }

    private int width;
    private int height;
    private Block[,] grid;
    private Dictionary<Block, Vector2Int> positions;
}
