using System;
using System.Collections.Generic;
using System.Linq;
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
        Debug.Assert(grid[block.GridPosition.x, block.GridPosition.y] == null, "Block position overlap!");
        grid[block.GridPosition.x, block.GridPosition.y] = block;

        positions.Add(block, block.GridPosition);

        block.isInMap = true;
    }

    public void Remove(Block block)
    {
        Debug.Assert(grid[positions[block].x, positions[block].y] == block, "Block position inconsistent!");
        grid[positions[block].x, positions[block].y] = null;

        positions.Remove(block);

        block.isInMap = false;
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

    private int width;
    private int height;
    private Block[,] grid;
    private Dictionary<Block, Vector2Int> positions;
}
