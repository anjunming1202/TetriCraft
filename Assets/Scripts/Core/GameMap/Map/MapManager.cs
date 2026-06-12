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
    [SerializeField] BlockSystemManager blockSystemManager;
    public int GridWidth => blockSystemManager.GridWidth;
    public int GridHeight => blockSystemManager.GridHeight;
    public IReadOnlyList<Block> Blocks => blockSystemManager.Blocks;


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


    // Random tick system
    public List<MapRandomTickBehaviourObject> mapRandomTickObjects;
    public int randomTickSelectionCount => GridWidth * GridHeight;

    
    // events
    public Action<MapManager, Block> OnGridPlace;


    // useful tag
    private bool isInitialised;


    public void Initialise()
    {
        if (isInitialised) return;

        Debug.Assert(blockSystemManager != null);
        Debug.Assert(waterManager != null);
        Debug.Assert(lavaManager != null);
        Debug.Assert(entityManager != null);
        Debug.Assert(particleManager != null);

        // Block subsystem initialise
        blockSystemManager.Initialise(this);
        blockSystemManager.OnGridPlace += HandleGridPlace;

        // Fluid subsystem initialise
        Debug.Assert(waterManager != null);
        Debug.Assert(lavaManager != null);
        fluidManager = new Dictionary<FluidID, FluidManager>()
        {
            { FluidID.Water, waterManager },
            { FluidID.Lava, lavaManager },
        };

        // Init map event
        OnGridPlace += waterManager.RequestBlockSqueeze; // squeeze fluid
        OnGridPlace += lavaManager.RequestBlockSqueeze;
        OnGridPlace += Flame.TryExtinguishBy; // try extinguish fire

        isInitialised = true;
    }

    public void Dispose()
    {
        // Init map event
        OnGridPlace -= waterManager.RequestBlockSqueeze; // squeeze fluid
        OnGridPlace -= lavaManager.RequestBlockSqueeze;
        OnGridPlace -= Flame.TryExtinguishBy; // try extinguish fire

        isInitialised = false;
    }

    public void PrepareNewMap(int width, int height, TetrisManager tetrisManager)
    {
        // Player reference
        PlayerID = tetrisManager.PlayerID;

        // Block subsystem
        blockSystemManager.PrepareNewMap(width, height);

        // Fluid subsystem
        waterManager.Init(this);
        lavaManager.Init(this);

        // Entity subsystem
        entityManager.Init(this);
    }

    public void ClearMap()
    {
        // Block
        blockSystemManager.ClearMap();

        // Fluid
        waterManager.ClearFluidSystem();
        lavaManager.ClearFluidSystem();

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
        waterManager.OnUpdate();
        lavaManager.OnUpdate();
        SpawnFluidConcretion();

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
        blockSystemManager.OnBlockRequestNCUpdate(GetBlock(gridPosition));
    }

    public void HandleBlockOnLockdown(Block block)
    {

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

    private void HandleGridPlace(MapManager map, Block block)
    {
        OnGridPlace?.Invoke(map, block);
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
                RequestSpawnBlock(spawnedBlock, waterElement.column, waterElement.lowerGridPosition);
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
