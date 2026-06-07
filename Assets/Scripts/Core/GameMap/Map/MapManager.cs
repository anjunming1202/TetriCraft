using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Data of blocks in the game
/// </summary>
public class MapManager : MonoBehaviour
{ 
    public PlayerID PlayerID { get; private set; }

    // map parameters
    public static float gravity = 15f;

    public int BoundaryWidth => boundaryWidth;
    public int BoundaryHeight => boundaryHeight;


    // Block system manager
    [SerializeField] public BlockGrid blockGrid;
    public Block this[int x, int y] => blockGrid[x, y];
    public int blockCount => blockGrid.blockCount;  // debug
    public BlockGrid grid => blockGrid;
    private int boundaryWidth;
    private int boundaryHeight;


    // Fluid system manager
    public Dictionary<FluidID, FluidManager> fluidManager;

    [SerializeField] private FluidManager waterManager;
    [SerializeField] private FluidManager lavaManager;

    [SerializeField] private AudioClip fizzSound;
    private static BlockID waterToLava = BlockID.Obsidian;
    private static BlockID lavaToWater = BlockID.Cobblestone;

    // Entity system manager
    [SerializeField] private EntityManager entityManager;

    // Particle system manager
    [SerializeField] private ParticleManager particleManager;


    // Redstone system
    public RedstoneManager RedstoneManager;


    // Block update system
    public BlockUpdateManager BlockUpdateManager;


    // Random tick system
    public List<MapRandomTickBehaviourObject> mapRandomTickObjects;
    public int randomTickSelectionCount => boundaryWidth * boundaryHeight;

    
    // events
    public Action<MapManager, Block> OnGridPlace;


    // Map update management
    private List<Block> blockList;
    private List<Block> blockUpdateBatch;
    private List<Block> blockDestroyBatch;
    public List<Block> blocks => blockList;
    public List<Block> batchBlocks => blockUpdateBatch;



    public void Initialise()
    {
        Debug.Assert(waterManager != null);
        Debug.Assert(lavaManager != null);
        fluidManager = new Dictionary<FluidID, FluidManager>()
        {
            { FluidID.Water, waterManager },
            { FluidID.Lava, lavaManager },
        };

        // Init map event
        OnGridPlace += waterManager.BlockSqueeze; // squeeze fluid
        OnGridPlace += lavaManager.BlockSqueeze;

        OnGridPlace += Flame.TryExtinguishBy; // try extinguish fire
    }

    public void PrepareNewMap(int width, int height, TetrisManager tetrisManager)
    {
        // Player reference
        PlayerID = tetrisManager.PlayerID;

        // Init block system
        this.boundaryWidth = width;
        this.boundaryHeight = height;
        blockGrid.Init(width, height + 5); // all null
        blockList = new List<Block>();
        blockUpdateBatch = new List<Block>();
        blockDestroyBatch = new List<Block>();

        BlockUpdateManager = new BlockUpdateManager(this); // block update system
        RedstoneManager = new RedstoneManager(this); // redstone system

        // Init fluid system
        waterManager.Init(this);
        lavaManager.Init(this);

        // Init entity system
        entityManager.Init(this);
    }

    public void ClearMap()
    {
        // block grid
        blockGrid.ClearAllBlocksWithDestroy();

        // Fluid
        waterManager.ClearFluidSystem();
        lavaManager.ClearFluidSystem();

        // Entity
        entityManager.Clear();

        // Particle
        particleManager.ClearAll();

        // Map update
        blockList = new();
        blockUpdateBatch = new();
        blockDestroyBatch = new();
        BlockUpdateManager = new(this);
        RedstoneManager = new(this);

        Debug.Log("Cleared all objects in the current map");
    }

    // map one frame update
    public void OnUpdate()
    {
        // Framely block update
        for (int i = 0; i < blockList.Count; i++)
        {
            if (blockList[i] != null)
                blockList[i].OnUpdate();
        }

        if (blockUpdateBatch.Count > 0)
        {
            BatchUpdateBlocks();
        }

        // Fluid Update
        waterManager.OnUpdate();
        lavaManager.OnUpdate();
        SpawnFluidConcretion();

        // Random tick behaviours
        RandomTick.InvokeRandomBehaviours(this);

        // Block update
        BlockUpdateManager.BlockUpdate();

        // Redstone
        RedstoneManager.RedstoneUpdate();

        // Entity
        entityManager.OnUpdate();
    }

    public Block GetBlock(int x, int y)
    {
        return grid.Get(x, y);
    }

    public IEnumerable<Block> GetAdjacentBlocks(int x, int y, bool includeSelf = false)
    {
        if (includeSelf)
            yield return GetBlock(x, y);
        foreach (Vector2Int offset in new Vector2Int[] {Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down})
            yield return GetBlock(x + offset.x, y + offset.y);
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

                AddNewBlock(block, gridPosition.x, gridPosition.y);

                block.transform.SetParent(tetromino.transform);
            }
    }

    public void SpawnBlock(Block block, int x, int y)
    {
        if (blockGrid[x, y] != null)
            blockGrid[x, y].OnReplacedBy(block);

        AddNewBlock(block, x, y);

        block.transform.SetParent(blockGrid.transform);

        block.OnLockdown();
    }

    public void DestroyBlock(Block block)
    {
        blockGrid.Remove(block);
        block.Destroyed();
        blockList.Remove(block);
    }

    public void RemoveBlock(Block block)
    {
        blockGrid.Remove(block);
        block.Removed();
        blockList.Remove(block);
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


    public void SpawnEntity(Entity entity, float x, float y)
    {
        entityManager.AddNewEntity(entity, x, y);

        entity.transform.SetParent(entityManager.transform, false);
    }

    public void KillEntity(Entity entity)
    {
        entityManager.KillEntity(entity);
    }

    public ParticleSystem SpawnParticle(ParticleSystem prefab, float x, float y)
    {
        Vector3 worldPosition = BoundaryDataManager.GetBoundaryData(PlayerID).MapToWorld(new Vector2(x, y));
        return particleManager.SpawnParticle(prefab, worldPosition);
    }

    public ParticleSystem SpawnParticle(ParticleSystem prefab, Vector3 worldPosition)
    {
        return particleManager.SpawnParticle(prefab, worldPosition);
    }

    public void DespawnParticle(ParticleSystem particle)
    {
        particleManager.DespawnParticle(particle);
    }



    private void AddToUpdateBatch(Block block)
    {
        if (block.isRemoved)
            return;
        if (!blockUpdateBatch.Contains(block))
            blockUpdateBatch.Add(block);
    }

    private void RemoveFromUpdateBatch(Block block)
    {
        if (blockUpdateBatch.Contains(block))
            blockUpdateBatch.Remove(block);
    }

    /// <summary>
    /// Add new block into the map
    /// </summary>
    private void AddNewBlock(Block block, int x, int y)
    {
        block.OnSpawn(this);

        block.SetPosition(x, y);

        blockGrid.Add(block);

        block.OnMoved += AddToUpdateBatch;
        block.OnRemoved += RemoveFromUpdateBatch;

        blockList.Add(block);

        //OnGridPlace?.Invoke(this, block); //
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



    // Check map data
    public bool IsBlockedWithoutCeiling(int x, int y)
    {
        return (!CheckInsideWithoutCeiling(x, y) || (CheckInsideBlockGrid(x, y) && !CheckEmpty(x, y)));
    }

    public bool IsBlockedInsideGrid(int x, int y)
    {
        return !CheckInsideBlockGrid(x, y) || !CheckEmpty(x, y);
    }



    public bool CheckInsideWithoutCeiling(int x, int y)
    {
        return blockGrid.CheckInsideWithoudCeiling(x, y);
    }
    public bool CheckInsideBlockGrid(int x, int y)
    {
        return blockGrid.CheckInsideGrid(x, y);
    }
    public bool CheckEmpty(int x, int y)
    {
        return blockGrid.CheckEmpty(x, y) || blockGrid[x, y].IsDummy;
    }

    public bool CheckRowFull(int row)
    {
        for (int column = 0; column < boundaryWidth; column++)
        {
            if (blockGrid[column, row] == null || !blockGrid[column, row].IsClearable())
                return false;
        }
        return true;
    }
    public bool CheckRowEmpty(int row)
    {
        for (int column = 0; column < boundaryWidth; column++)
        {
            if (blockGrid[column, row] != null && blockGrid[column, row].isLocked)
                return false;
        }
        return true;
    }

    public bool IsInsideGrid(int x, int y)
    {
        return x >= 0 && x < boundaryWidth && y >= 0 && y < boundaryHeight;
    }

    public bool CheckMapEmpty()
    {
        for (int row = 0; row < boundaryHeight - 1; row++)
        {
            if (!CheckRowEmpty(row))
                return false;
        }
        return true;
    }
}
