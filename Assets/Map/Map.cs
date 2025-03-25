using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
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

    // Blocks
    public Block[,] blockMap;
    public List<Block> blocks = new List<Block>();

    // Map Boundary Data
    public int width => MapBoundaryData.Instance.width;
    public int height => MapBoundaryData.Instance.height;
    public Block this[int x, int y]
    {
        get => blockMap[x, y];
    }

    // Map recorded data
    public int lastClearLineCount = 0;
    public int combo = 0;

    // Debugging
    public int blockCount
    {
        get
        {
            int blockCount = 0;
            foreach (var t in blockMap)
            {
                if (t != null)
                {
                    blockCount++;
                }
            }
            return blockCount;
        }
    }






    // Block Data Editting API
    // Update map & block data

    // Add
    public void Add(Block block, int x, int y)
    {
        // block can only be added once
        if (block.isInMap)
        {
            Debug.LogError($"Try to add a block twice at {x}, {y}");
            return;
        }
        else
            block.isInMap = true;

        // check for valid position
        if (!CheckInside(x, y) || !CheckEmpty(x, y))
        {
            Debug.LogError($"Try to add a block to an invalid position {x}, {y}");
            return;
        }

        // map data
        blockMap[x, y] = block;
        blocks.Add(block);

        // block pos
        block.position = new Vector2Int(x, y);
    }

    // Move
    public bool MoveTo(Block block, int x, int y)
    {
        Vector2Int from = block.position;

        // for debug
        Debug.Assert(blockMap[from.x, from.y] == block, $"block position not consistent with the map, block position {from.x}, {from.y}");

        if (!CheckInside(x, y) || !CheckEmpty(x, y))
        {
            return false;
        }

        blockMap[from.x, from.y] = null;
        blockMap[x, y] = block;
        block.position = new Vector2Int(x, y);
        return true;
    }

    // Remove
    public Block Remove(int x, int y)
    {
        Block block = blockMap[x, y];
        if (block == null)
            return null;
        blockMap[x, y] = null;
        block.isInMap = false;
        return block;
    }
    public void Destroy(int x, int y)
    {
        // for debug
        Debug.Assert(blockMap[x, y] != null, $"Try to destroy at an empty position {x}, {y}");

        blockMap[x, y].Destroy();
        blockMap[x, y] = null;
    }



    /*//

    // Set
    /// <summary>
    /// Place a block to an empty position
    /// </summary>
    public void SetBlock(Block block, int x, int y)
    {
        // check for debuging
        Debug.Assert(blockMap[x, y] == null, $"Trying to place a block to an non-empty position {x}, {y}");

        blockMap[x, y] = block;
        block.SetPosition(new Vector2Int(x, y));
        if (!block.isInMap)
            block.isInMap = true;
    }
    /// <summary>
    /// Place a block to an empty position
    /// </summary>
    public void SetBlock(Block block, Vector2Int pos) => SetBlock(block, pos.x, pos.y);
    /// <summary>
    /// Set tetromino blocks onto the map
    /// </summary>
    public void SetTetromino(Tetromino tetromino, int x, int y)
    {
        // Set data of tetromino self
        tetromino.SetPosition(new Vector2Int(x, y));

        // Set data of blocks (in the map + block self)
        for (int c = 0; c < tetromino.size; c++)
            for (int r = 0; r < tetromino.size; r++)
            {
                Block block = tetromino[r, c];
                if (block != null)
                {
                    // block position = tetromino position + local position in the tetromino
                    Vector2Int blockPosition = tetromino.LocalToMap(r, c);
                    SetBlock(block, blockPosition);
                }
            }
    }
    /// <summary>
    /// Set tetromino blocks onto the map
    /// </summary>
    public void SetTetromino(TetrominoManager tetromino, Vector2Int pos) => SetTetromino(tetromino, pos.x, pos.y);
    /// <summary>
    /// Place down the block onto the map according to its data position
    /// </summary>
    public void PlaceBlockDown(Block block)
    {
        // check for debuging
        Debug.Assert(blockMap[block.MapPosition.x, block.MapPosition.y] == null, $"Trying to PLACE DOWN a block to an non-empty position {block.MapPosition.x}, {block.MapPosition.y}");

        blockMap[block.MapPosition.x, block.MapPosition.y] = block;
    }
    /// <summary>
    /// Place down the tetromino according to its position data
    /// </summary>
    public void PlaceTetrominoDown(Tetromino tetromino)
    {
        // Set data of blocks (in the map + block self)
        foreach (Block block in tetromino.blocks)
        {
            PlaceBlockDown(block);
        }
    }



    // Remove
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
    public void RemoveBlock(Block block)
    {
        // checking for debug
        Debug.Assert(blockMap[block.MapPosition.x, block.MapPosition.y] == block, "Block position inconsistent with map data");

        Remove(block.MapPosition);
    }
    /// <summary>
    /// Remove tetromino blocks on the map, but not destroy
    /// </summary>
    public void RemoveTetromino(Tetromino tetromino)
    {
        foreach (Block block in tetromino.blocks)
        {
            RemoveBlock(block);
        }
    }



    // Move
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
        PlaceBlockDown(block);
    }
    /// <summary>
    /// Move a group of blocks at once
    /// </summary>
    public void MoveBlocksBy(Block[] blocks, int x, int y)
    {
        // Remove all original block first
        foreach (Block block in blocks)
        {
            RemoveBlock(block);
        }
        // Then move and place blocks
        foreach (Block block in blocks)
        {
            // Move block
            block.MoveBy(x, y);
            // Place down block with moving
            PlaceBlockDown(block);
        }
    }
    public void MoveTetrominoTo(Tetromino tetromino, Vector2Int to)
    {
        // Remove original blocks
        RemoveTetromino(tetromino);

        // Move tetromino -> set position
        tetromino.MoveTo(to);

        // Place down tetromino blocks with moving
        PlaceTetrominoDown(tetromino);
    }*/






    // Check map data
    /// <summary>
    /// Check for bottom, left, and right boundaries
    /// </summary>
    public bool CheckInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0;
    }
    public bool CheckEmpty(int x, int y)
    {
        return blockMap[x, y] == null;
    }

    public bool CheckRowFull(int row)
    {
        for (int column = 0; column < width; column++)
        {
            if (blockMap[column, row] == null || !blockMap[column, row].isLocked)
                return false;
        }
        return true;
    }
    public bool CheckRowEmpty(int row)
    {
        for (int column = 0; column < width; column++)
        {
            if (blockMap[column, row] != null && blockMap[column, row].isLocked)
                return false;
        }
        return true;
    }
    public bool CheckMapFull()
    {
        return !CheckRowEmpty(height);
    }
    public bool CheckMapEmpty()
    {
        for (int row = 0; row < height - 1; row++)
        {
            if (!CheckRowEmpty(row))
                return false;
        }
        return true;
    }
}
