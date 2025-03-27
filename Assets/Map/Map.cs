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
public class Map : MonoBehaviour
{
    public void NewMap(int width, int height)
    {
        this.width = width;
        this.height = height;
        blockGrid = new BlockGrid(width, height); // all null
        blockUpdateBatch = new List<Block>();
    }
    public Block this[int x, int y] => blockGrid[x, y];
    public int Width => width;
    public int Height => height;
    public int blockCount => blockGrid.blockCount;  // debug

    public void SpawnBlock(Block block, int x, int y)
    {
        block.SetPosition(x, y);
        blockGrid.Add(block);
        block.OnPositionChanged += AddToUpdateBatch;
    }

    public void DestroyBlock(Block block)
    {
        blockGrid.Remove(block);
        block.Destroy();
    }

    public void RemoveBlock(Block block)
    {
        blockGrid.Remove(block);
        block.Remove();
    }

    public void BatchUpdateBlocks()
    {
        foreach (Block block in blockUpdateBatch)
        {
            blockGrid.Remove(block);
        }
        foreach (Block block in blockUpdateBatch)
        {
            blockGrid.Add(block);
        }
        blockUpdateBatch.Clear();
    }

    private void Update()
    {
        if (blockUpdateBatch.Count > 0)
        {
            BatchUpdateBlocks();
        }
    }

    private void AddToUpdateBatch(Block block)
    {
        blockUpdateBatch.Add(block);
    }

    // Blocks
    private BlockGrid blockGrid;

    // Map Boundary Data
    private int width;
    private int height;

    // Map update
    private List<Block> blockUpdateBatch;




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
        return blockGrid[x, y] == null;
    }

    public bool CheckRowFull(int row)
    {
        for (int column = 0; column < width; column++)
        {
            if (blockGrid[column, row] == null || !blockGrid[column, row].isLocked)
                return false;
        }
        return true;
    }
    public bool CheckRowEmpty(int row)
    {
        for (int column = 0; column < width; column++)
        {
            if (blockGrid[column, row] != null && blockGrid[column, row].isLocked)
                return false;
        }
        return true;
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
