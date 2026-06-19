using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BlockGrid : MonoBehaviour, IReadonlyBlockGrid
{
    private Block[,] grid;
    private readonly Dictionary<Block, Vector2Int> positions = new();
    private readonly List<Block> blocks = new();

    public IReadOnlyList<Block> Blocks => blocks;
    public int GridHeight => grid.GetLength(1);
    public int GridWidth => grid.GetLength(0);
    public Vector2Int GetPosition(Block block) => positions[block];


    public void Init(int width, int height)
    {
        // Init
        grid = new Block[width, height];
    }

    public bool TryAdd(Block block, Vector2Int position)
    {
        // null value check
        if (block == null)
        {
            //Debug.LogError($"Block to be added is a null, fail to add");
            return false;
        }

        // occupancy check
        int x = position.x;
        int y = position.y;
        if (!IsEmpty(x, y))
        {
            //Debug.LogError($"Position {(x, y)} is already occupied, fail to add");
            return false;
        }

        // in grid check
        if (positions.ContainsKey(block))
        {
            //Debug.LogError($"Block {block} already exists in grid.");
            return false;
        }

        // successful add
        grid[x, y] = block;
        positions.Add(block, position);
        blocks.Add(block);

        return true;
    }

    public bool TryRemove(Block block)
    {
        // null value check
        if (block == null)
        {
            //Debug.LogError("Block to be removed is a null, fail to remove");
            return false;
        }

        // in grid check
        if (!positions.TryGetValue(block, out Vector2Int removedPos))
        {
            //Debug.LogError($"Block {block} was not added to the grid, fail to remove");
            return false;
        }

        // successful remove
        grid[removedPos.x, removedPos.y] = null;
        positions.Remove(block);
        blocks.Remove(block);

        return true;
    }

    public bool TryMove(Block block, Vector2Int targetPosition)
    {
        // null value check
        if (block == null)
        {
            //Debug.LogError("Block to be removed is a null, fail to move");
            return false;
        }

        // occupancy check
        int x = targetPosition.x;
        int y = targetPosition.y;
        if (!IsEmpty(x, y))
        {
            //Debug.LogError($"Position {(x, y)} is occupied, fail to move");
            return false;
        }

        // in grid check
        if (!positions.TryGetValue(block, out Vector2Int originalPos))
        {
            //Debug.LogError($"Block {block} was not added to the grid, fail to move");
            return false;
        }

        // successful move
        grid[originalPos.x, originalPos.y] = null;
        grid[x, y] = block;
        positions[block] = targetPosition;

        return true;
    }

    public bool TryMoveBatch(Dictionary<Block, Vector2Int> moves)
    {
        Dictionary<Block, Vector2Int> successfulRemoves = new();
        foreach (var (block, targetPosition) in moves)
        {
            Vector2Int lastPosition = positions[block];
            if (TryRemove(block))
                successfulRemoves.Add(block, lastPosition);
            else
            {
                foreach (var (removedBlock, removedPosition) in successfulRemoves)
                {
                    TryAdd(removedBlock, removedPosition);
                }
                //Debug.LogError("Try move batch failed!");
                return false;
            }
        }
        HashSet<Block> successfulAdds = new();
        foreach (var (block, targetPosition) in moves)
        {
            if (TryAdd(block, targetPosition))
                successfulAdds.Add(block);
            else
            {
                foreach (var addedBlock in successfulAdds)
                {
                    TryRemove(addedBlock);
                }
                foreach (var (removedBlock, removedPosition) in successfulRemoves)
                {
                    TryAdd(removedBlock, removedPosition);
                }
                //Debug.LogError("Try move batch failed!");
                return false;
            }
        }
        return true;
    }

    public void Clear()
    {
        grid = new Block[GridWidth, GridHeight];
        positions.Clear();
        blocks.Clear();

        Debug.Log("Cleared all blocks in the grid");
    }

    public Block Get(int x, int y)
    {
        if (CheckInsideGrid(x, y))
            return grid[x, y];
        return null;
    }
    public Block Get(Vector2Int pos) => Get(pos.x, pos.y);

    public bool TryGet(Vector2Int pos, out Block block)
    {
        block = Get(pos.x, pos.y);
        return block != null;
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

    public bool IsEmpty(int x, int y)
    {
        Debug.Assert(x<GridWidth && y<GridHeight, $"width: {GridWidth}, x = {x}, height: {GridHeight}, y = {y}");
        return grid[x, y] == null;
    }
    public bool IsEmpty(Vector2Int pos) => IsEmpty(pos.x, pos.y);

    public bool IsOccupied(int x, int y)
    {
        return !IsEmpty(x, y);
    }
}

public interface IReadonlyBlockGrid
{
    public int GridHeight { get; }
    public int GridWidth { get; }
    public Block Get(int x, int y);
    public Block Get(Vector2Int pos) => Get(pos.x, pos.y);

    public bool TryGet(Vector2Int pos, out Block block);

    public bool Contains(Block block);

    public bool CheckInsideGrid(int x, int y);

    public bool CheckInsideWithoudCeiling(int x, int y);

    public bool IsEmpty(int x, int y);
    public bool IsEmpty(Vector2Int pos) => IsEmpty(pos.x, pos.y);

    public bool IsOccupied(int x, int y);
}