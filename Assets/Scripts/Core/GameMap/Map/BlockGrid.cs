using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BlockGrid : MonoBehaviour
{
    private Block[,] grid;
    private Dictionary<Block, Vector2Int> positions;

    public Block this[int x, int y] => grid[x, y];
    public int GridHeight => grid.GetLength(1);
    public int GridWidth => grid.GetLength(0);
    public int blockCount => positions.Count;   // debug


    public void Init(int width, int height)
    {
        // Init
        grid = new Block[width, height];
        positions = new Dictionary<Block, Vector2Int>();
    }

    public bool TryAdd(Block block)
    {
        // null value check
        if (block == null)
        {
            Debug.LogError($"Block to be added is a null, fail to add");
            return false;
        }

        // occupancy check
        int x = block.GridPosition.x;
        int y = block.GridPosition.y;
        if (!CheckEmpty(x, y))
        {
            Debug.LogError($"Position {(x, y)} is already occupied, fail to add");
            return false;
        }

        // in grid check
        if (positions.ContainsKey(block))
        {
            Debug.LogError($"Block {block} already exists in grid.");
            return false;
        }

        // successful add
        grid[x, y] = block;
        positions.Add(block, block.GridPosition);

        return true;

        //block.isInMap = true;

        // grid block update
        //BlockUpdateManager.OnNeighbourChangedBlockUpdate(this, new Vector2Int(x, y), block);
    }

    public bool TryRemove(Block block)
    {
        // null value check
        if (block == null)
        {
            Debug.LogError("Block to be removed is a null, fail to remove");
            return false;
        }

        // in grid check
        if (!positions.TryGetValue(block, out Vector2Int removedPos))
        {
            Debug.LogError($"Block {block} was not added to the grid, fail to remove");
            return false;
        }

        // successful remove
        grid[removedPos.x, removedPos.y] = null;
        positions.Remove(block);

        return true;

        //block.isInMap = false;

        // grid block update
        //BlockUpdateManager.OnNeighbourChangedBlockUpdate(this, removedPos, block);
    }

    public bool TryMove(Block block)
    {
        // null value check
        if (block == null)
        {
            Debug.LogError("Block to be removed is a null, fail to move");
            return false;
        }

        // occupancy check
        int x = block.GridPosition.x;
        int y = block.GridPosition.y;
        if (!CheckEmpty(x, y))
        {
            Debug.LogError($"Position {(x, y)} is occupied, fail to move");
            return false;
        }

        // in grid check
        if (!positions.TryGetValue(block, out Vector2Int originalPos))
        {
            Debug.LogError($"Block {block} was not added to the grid, fail to move");
            return false;
        }

        // successful move
        grid[originalPos.x, originalPos.y] = null;
        grid[x, y] = block;
        positions[block] = block.GridPosition;

        return true;
    }

    public void Clear()
    {
        grid = new Block[GridWidth, GridHeight];
        positions = new Dictionary<Block, Vector2Int>();

        Debug.Log("Cleared all blocks in the grid");
    }

    public void BatchMove(List<Block> blocks)
    {
        foreach (Block block in blocks)
        {
            TryRemove(block);
        }

        // Reinsert them at their new positions.
        foreach (Block block in blocks)
        {
            TryAdd(block);
        }
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
        return x >= 0 && x < GridWidth && y >= 0 && y < GridHeight;
    }

    public bool CheckInsideWithoudCeiling(int x, int y)
    {
        return x >= 0 && x < GridWidth && y >= 0;
    }

    public bool CheckEmpty(int x, int y)
    {
        Debug.Assert(x<GridWidth && y<GridHeight, $"width: {GridWidth}, x = {x}, height: {GridHeight}, y = {y}");
        return grid[x, y] == null;
    }
}
