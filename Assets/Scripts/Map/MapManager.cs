using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Collections.AllocatorManager;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// Data of blocks in the game
/// </summary>
public class MapManager : MonoBehaviour
{ 
    public static float gravity = 15f;

    public static FluidManager WaterManager;
    public static FluidManager LavaManager;

    public List<MapRandomTickBehaviourObject> mapRandomTickObjects;
    public int randomTickSelectionCount => width * height;

    public Block this[int x, int y] => blockGrid[x, y];
    public int Width => width;
    public int Height => height;

    public int blockCount => blockGrid.blockCount;  // debug
    public BlockGrid grid => blockGrid;
    public List<Block> blocks => blockList;
    public List<Block> batchBlocks => blockUpdateBatch;

    public Action<MapManager, Block> OnGridPlace;

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

        OnGridPlace += waterManager.BlockSqueeze;
        OnGridPlace += lavaManager.BlockSqueeze;
    }

    public Block GetBlock(int x, int y)
    {
        if (CheckInsideGrid(x, y))
            return blockGrid[x, y];
        return null;
    }

    public bool IsBlocked(int x, int y)
    {
        return !CheckInside(x, y) || !CheckEmpty(x, y);
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
                block.OnSpawn(this);
                AddNewBlock(block, gridPosition.x, gridPosition.y, false);
                block.transform.SetParent(tetromino.transform);
            }
    }

    public void SpawnBlock(Block block, int x, int y)
    {
        if (blockGrid[x, y] != null)
            blockGrid[x, y].OnReplacedBy(block);

        block.OnSpawn(this);
        AddNewBlock(block, x, y, true);
    }

    public void DestroyBlock(Block block)
    {
        blockGrid.Remove(block);
        block.Destroy();
        blockList.Remove(block);
    }

    public void RemoveBlock(Block block)
    {
        blockGrid.Remove(block);
        block.Remove();
        blockList.Remove(block);
    }

    public void OnUpdate()
    {
        // Block map
        for (int i = 0; i < blockList.Count; i++)
        {
            blockList[i].OnUpdate();
        }

        // Fluid map
        waterManager.OnUpdate();
        lavaManager.OnUpdate();
        SpawnFluidConcretion();

        // Random tick behaviours
        RandomTick.InvokeRandomBehaviours(this);
    }

    public void BatchUpdateBlocks()
    {
        foreach (Block block in blockUpdateBatch)
        {
            blockGrid.Remove(block);
        }
        foreach (Block block in blockUpdateBatch)
        {
            // if dummy
            int x = block.GridPosition.x;
            int y = block.GridPosition.y;
            if (blockGrid[x, y] != null && blockGrid[x, y].IsDummy)
            {
                RemoveBlock(blockGrid[x, y]);
            }
            blockGrid.Add(block);
        }
        foreach (Block block in blockUpdateBatch)
        {
            OnGridPlace?.Invoke(this, block);
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
    private void AddNewBlock(Block block, int x, int y, bool lockdownState)
    {
        block.SetPosition(x, y);

        blockGrid.Add(block);

        block.OnPositionChanged += AddToUpdateBatch;

        blockList.Add(block);

        if (lockdownState)
            block.OnLockdown();

        //OnGridPlace?.Invoke(this, block);
    }

    private void SpawnFluidConcretion()
    {
        for (int i =  waterManager.fluidSystem.elements.Count - 1; i >= 0; i--)
        {
            FluidElement waterElement = waterManager.fluidSystem.elements[i];
            List<FluidElement> collidedLavaElements = lavaManager.fluidSystem.GetCollidedElements(waterElement);
            FluidElement lavaElement = collidedLavaElements.Count > 0 ? collidedLavaElements[0] : null;
            if (lavaElement != null && lavaElement.amount > 0 && waterElement.amount > 0)
            {
                // Spawn block
                int concretionAmount = Mathf.Min(waterElement.upperLevel, lavaElement.upperLevel) - Mathf.Max(waterElement.lowerLevel, lavaElement.lowerLevel);
                waterElement.amount -= concretionAmount;
                lavaElement.amount -= concretionAmount;

                Block spawnedBlock;
                if (waterElement.hasFlown)
                    spawnedBlock = BlockSpawner.NewBlock(waterToLava);
                else if (lavaElement.hasFlown)
                    spawnedBlock = BlockSpawner.NewBlock(lavaToWater);
                else //
                    spawnedBlock = BlockSpawner.NewBlock(waterToLava);

                spawnedBlock.GetComponent<BlockSoundManager>().placedSounds = new AudioClip[] { fizzSound };
                SpawnBlock(spawnedBlock, waterElement.column, waterElement.lowerGridPosition);
            }
        }
    }

    // Blocks
    private BlockGrid blockGrid;

    // Fluid
    [SerializeField] private FluidManager waterManager;
    [SerializeField] private FluidManager lavaManager;

    [SerializeField] private AudioClip fizzSound;
    private BlockID waterToLava = BlockID.Obsidian;
    private BlockID lavaToWater = BlockID.Cobblestone;

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
        if (y >= height)
            return true;

        return blockGrid[x, y] == null || blockGrid[x, y].IsDummy;
    }
    public bool CheckInsideGrid(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool CheckRowFull(int row)
    {
        for (int column = 0; column < width; column++)
        {
            if (blockGrid[column, row] == null || !blockGrid[column, row].IsClearable())
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

    public bool IsInsideGrid(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
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
