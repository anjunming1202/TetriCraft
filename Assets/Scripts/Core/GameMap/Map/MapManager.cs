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


    // Block system manager
    [SerializeField] private BlockSystemManager blockSystemManager;
    public int GridWidth => blockSystemManager.GridWidth;
    public int GridHeight => blockSystemManager.GridHeight;
    public IReadOnlyList<Block> Blocks => blockSystemManager.Blocks;
    public BlockNCUpdateManager BlockNCUpdateManager => blockSystemManager.BlockNCUpdateManager;
    public RedstoneManager RedstoneManager => blockSystemManager.RedstoneManager;


    // Fluid system manager
    [SerializeField] private FluidSystemManager fluidSystemManager;
    public FluidSystemManager FluidSystem => fluidSystemManager;


    // Entity system manager
    [SerializeField] private EntityManager entityManager;


    // Particle system manager
    [SerializeField] private ParticleManager particleManager;


    // Random tick system
    public List<MapRandomTickBehaviourObject> mapRandomTickObjects;
    public int randomTickSelectionCount => GridWidth * GridHeight;

    
    // events
    public delegate void MapBlockEvent(MapManager map, Block block);
    public event MapBlockEvent OnGridBlockPlaced;
    public event MapBlockEvent OnBlockLockdown;


    // useful tag
    private bool isInitialised;


    public void Initialise()
    {
        if (isInitialised) return;

        Debug.Assert(blockSystemManager != null);
        Debug.Assert(fluidSystemManager != null);
        Debug.Assert(entityManager != null);
        Debug.Assert(particleManager != null);

        // Block subsystem initialise
        blockSystemManager.Initialise(this);

        // Fluid subsystem initialise
        fluidSystemManager.Initialise(this);

        isInitialised = true;
    }

    public void Dispose()
    {
        blockSystemManager.Dispose();

        fluidSystemManager.Dispose();

        isInitialised = false;
    }

    public void PrepareNewMap(int width, int height, TetrisManager tetrisManager)
    {
        // Player reference
        PlayerID = tetrisManager.PlayerID;

        // Block subsystem
        blockSystemManager.PrepareNewMap(width, height);

        // Fluid subsystem
        fluidSystemManager.PrepareNew(this);

        // Entity subsystem
        entityManager.Init(this);
    }

    public void ClearMap()
    {
        // Block
        blockSystemManager.ClearMap();

        // Fluid
        fluidSystemManager.Clear();

        // Entity
        entityManager.Clear();

        // Particle
        particleManager.ClearAll();

        Debug.Log("Cleared all objects in the current map");
    }

    // map one frame update
    public void OnUpdate()
    {
        // Block grid update
        blockSystemManager.OnUpdate();

        // Fluid update
        fluidSystemManager.OnUpdate();

        // Random tick behaviours
        RandomTick.InvokeRandomBehaviours(this);

        // Entity update
        entityManager.OnUpdate();
    }

    public Block GetBlock(int x, int y)
    {
        return blockSystemManager.GetBlock(x, y);
    }
    public Block GetBlock(Vector2Int gridPosition)
    {
        return blockSystemManager.GetBlock(gridPosition.x, gridPosition.y);
    }

    public IEnumerable<Block> GetAdjacentBlocks(int x, int y, bool includeSelf = false)
    {
        return blockSystemManager.GetAdjacentBlocks(x, y, includeSelf);
    }

    public void SpawnTetromino(MapTetromino tetromino)
    {
        blockSystemManager.RequestSpawnTetromino(tetromino);
    }

    public void RequestSpawnBlock(Block block, int x, int y)
    {
        blockSystemManager.RequestSpawnBlock(block, x, y);
    }

    public void RequestMoveBlock(Block block, int x, int y, bool animated = true)
    {
        blockSystemManager.RequestMoveBlock(block, x, y, animated);
    }

    public void RequestDestroyBlock(Block block)
    {
        blockSystemManager.RequestDestroyBlock(block);
        block.OnRequestingDestroy();
    }

    public void RequestRemoveBlock(Block block)
    {
        blockSystemManager.RequestRemoveBlock(block);
        block.OnRequestingRemove();
    }

    public void ImmediatelyProcessGridPendingUpdates()
    {
        blockSystemManager.ImmediatelyProcessGridPendingUpdates();
    }

    public void ReparentBlock(Block block)
    {
        block.transform.SetParent(blockSystemManager.blockRoot, true);
    }

    public void RequestNCUpdate(Vector2Int gridPosition)
    {
        blockSystemManager.OnBlockRequestSendingNCUpdate(GetBlock(gridPosition));
    }

    public void HandleOnGridBlockPlaced(Block block)
    {
        OnGridBlockPlaced?.Invoke(this, block);
        Flame.TryExtinguishBy(this, block); // try extinguish fire, move to fire system in the future
    }

    public void ImmediateBlockSqueezeFluids(Block block)
    {
        fluidSystemManager.ImmediateBlockSqueeze(this, block);
    }


    public void RequestSpawnEntity(Entity entity, float x, float y)
    {
        entityManager.RequestAddEntity(entity, x, y);
    }

    public void RequestKillEntity(Entity entity)
    {
        entityManager.RequestKillEntity(entity);
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
        return blockSystemManager.CheckInsideWithoudCeiling(x, y);
    }
    public bool CheckInsideBlockGrid(int x, int y)
    {
        return blockSystemManager.CheckInsideGrid(x, y);
    }
    public bool CheckEmpty(int x, int y)
    {
        return blockSystemManager.CheckEmpty(x, y) || blockSystemManager.GetBlock(x, y).IsDummy;
    }

    public bool CheckRowFull(int row)
    {
        return blockSystemManager.CheckRowFull(row);
    }
    public bool CheckRowEmpty(int row)
    {
        return blockSystemManager.CheckRowEmpty(row);
    }

    public bool CheckMapEmpty()
    {
        return blockSystemManager.CheckMapEmpty();
    }

    public bool ContainBlock(Block block)
    {
        return blockSystemManager.Contains(block);
    }
}
