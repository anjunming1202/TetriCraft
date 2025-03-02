using System.Drawing;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

/// <summary>
/// Data of blocks in the game
/// </summary>
public class Map
{
    public Map()
    {
        blockMap = new Block[width, height + 5]; // all null
    }

    public Block[,] blockMap;

    // Map Boundary Data
    public int width => MapBoundaryData.Instance.width;
    public int height => MapBoundaryData.Instance.height;
    public Block this[int x, int y]
    {
        get => blockMap[x, y];
    }



    // Block Data Editting API
    // Ensure each change position for block syncs in both block and map data

    // Add block
    /// <summary>
    /// Place a block to an empty position
    /// </summary>
    public void PlaceBlock(Block block, int x, int y)
    {
        // check for debuging
        Debug.Assert(blockMap[x, y] == null, "Trying to place a block to an non-empty position");

        blockMap[x, y] = block;
        block.SetPosition(new Vector2Int(x, y));
        if (!block.isInMap)
            block.isInMap = true;
    }
    /// <summary>
    /// Place a block to an empty position
    /// </summary>
    public void PlaceBlock(Block block, Vector2Int pos) => PlaceBlock(block, pos.x, pos.y);
    /// <summary>
    /// Place a block onto the map according to its data position
    /// </summary>
    public void PlaceBlock(Block block) => PlaceBlock(block, block.MapPosition);

    // Remove block
    /// <summary>
    /// Set one position to null
    /// </summary>
    public void Remove(int x, int y)
    {
        blockMap[x, y] = null;
    }
    /// <summary>
    /// Set one position to null
    /// </summary>
    public void Remove(Vector2Int pos) => Remove(pos.x, pos.y);
    /// <summary>
    /// Set one position to null according to the block data position
    /// </summary>
    public void Remove(Block block)
    {
        // checking for debug
        Debug.Assert(blockMap[block.MapPosition.x, block.MapPosition.y] == block, "Block position inconsistent with map data");

        Remove(block.MapPosition);
    }

    // Move block
    /// <summary>
    /// Move a block to another position at once, use when moving single block only
    /// </summary>
    public void MoveBlockTo(Block block, int x, int y) => MoveBlockTo(block, new Vector2Int(x, y));
    /// <summary>
    /// Move a block to another position at once, use when moving single block only
    /// </summary>
    public void MoveBlockTo(Block block, Vector2Int to)
    {
        // Remove original block
        Remove(block.MapPosition);

        // Move block
        block.MoveTo(to); // set position

        // Place down block with moving
        PlaceBlock(block);
    }
    /// <summary>
    /// Move a group of blocks at once
    /// </summary>
    public void MoveBlocksBy(Block[] blocks, int x, int y)
    {
        // Remove all original block first
        foreach (Block block in blocks)
        {
            Remove(block);
        }
        // Then move and place blocks
        foreach (Block block in blocks)
        {
            // Move block
            block.MoveBy(x, y);
            // Place down block with moving
            PlaceBlock(block);
        }
    }

    // Destroy block
    /// <summary>
    /// Destroy and remove a block
    /// </summary>
    public void Destroy(int x, int y)
    {
        blockMap[x, y].Destroy();
        Remove(x, y);
    }

    // Check map data
    /// <summary>
    /// Check for bottom, left, and right boundaries
    /// </summary>
    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0;
    }
    public bool IsEmpty(int x, int y)
    {
        return blockMap[x, y] == null;
    }
    public bool IsRowFull(int row)
    {
        for (int column = 0; column < width; column++)
        {
            if (blockMap[column, row] == null)
                return false;
        }
        return true;
    }
    public bool IsRowEmpty(int row)
    {
        for (int column = 0; column < width; column++)
        {
            if (blockMap[column, row] != null)
                return false;
        }
        return true;
    }
}
