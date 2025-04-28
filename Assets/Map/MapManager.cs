using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Collections.AllocatorManager;

/// <summary>
/// Data of blocks in the game
/// </summary>
public class MapManager : MonoBehaviour
{ 
    static public FluidManager WaterManager;
    static public FluidManager LavaManager;

    public Block this[int x, int y] => blockGrid[x, y];
    public int Width => width;
    public int Height => height;

    public int blockCount => blockGrid.blockCount;  // debug
    public BlockGrid grid => blockGrid;
    public List<Block> blocks => blockList;
    public List<Block> batchBlocks => blockUpdateBatch;



    public void NewMap(int width, int height)
    {
        this.width = width;
        this.height = height;
        blockGrid = new BlockGrid(width, height); // all null
        blockList = new List<Block>();
        blockUpdateBatch = new List<Block>();
        blockDestroyBatch = new List<Block>();

        WaterManager = waterManager;
        LavaManager = lavaManager;
    }

    public void SpawnTetromino(MapTetromino tetromino)
    {
        for (int r = 0; r < tetromino.size; r++)
            for (int c = 0; c < tetromino.size; c++)
            {
                Block block = tetromino.shape[r, c];
                if (block == null)
                    continue;
                Vector2Int gridPosition = tetromino.LocalToMap(r, c);
                AddBlock(block, gridPosition.x, gridPosition.y, false);
                block.transform.SetParent(tetromino.transform);
            }
    }

    public void SpawnBlock(Block block, int x, int y)
    {
        AddBlock(block, x, y, true);
        block.transform.SetParent(transform);
    }

    public void DestroyBlock(Block block)
    {
        blockGrid.Remove(block);
        block.Destroy(this);
        blockList.Remove(block);
    }

    public void RemoveBlock(Block block)
    {
        blockGrid.Remove(block);
        block.Remove(this);
        blockList.Remove(block);
    }

    public void OnUpdateBlocks()
    {
        for (int i = 0; i < blockList.Count; i++)
        {
            blockList[i].OnUpdate(this);
        }
        waterManager.OnUpdate(this);
        lavaManager.OnUpdate(this);
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
        if (!blockUpdateBatch.Contains(block))
            blockUpdateBatch.Add(block);
    }

    /// <summary>
    /// Add new block into the map
    /// </summary>
    private void AddBlock(Block block, int x, int y, bool lockdownState)
    {
        block.SetPosition(x, y);

        blockGrid.Add(block);

        block.OnPositionChanged += AddToUpdateBatch;

        blockList.Add(block);

        if (lockdownState)
            block.OnLockdown(this);
    }

    // Blocks
    private BlockGrid blockGrid;

    // Fluid
    [SerializeField] private FluidManager waterManager;
    [SerializeField] private FluidManager lavaManager;

    // Map Boundary Data
    private int width;
    private int height;

    // Map update
    private List<Block> blockList;
    private List<Block> blockUpdateBatch;
    private List<Block> blockDestroyBatch;




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
            if (blockGrid[column, row] == null || !blockGrid[column, row].isClearable)
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
