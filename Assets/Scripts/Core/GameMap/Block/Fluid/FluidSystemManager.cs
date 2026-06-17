using System.Collections.Generic;
using UnityEngine;

public class FluidSystemManager : MonoBehaviour
{
    [SerializeField] private FluidManager waterManager;
    [SerializeField] private FluidManager lavaManager;

    [SerializeField] private AudioClip fizzSound;
    private static readonly BlockID waterToLava = BlockID.Obsidian;
    private static readonly BlockID lavaToWater = BlockID.Cobblestone;

    private MapManager map;
    private Dictionary<FluidID, FluidManager> fluidManagers;

    public FluidManager this[FluidID id] => fluidManagers[id];

    public void Initialise(MapManager mapOwner)
    {
        map = mapOwner;
        waterManager.Initialise(mapOwner);
        lavaManager.Initialise(mapOwner);
        fluidManagers = new Dictionary<FluidID, FluidManager>
        {
            { FluidID.Water, waterManager },
            { FluidID.Lava,  lavaManager  },
        };
    }

    public void Dispose()
    {
        waterManager.Dispose();
        lavaManager.Dispose();
    }

    public void PrepareNew(MapManager mapOwner)
    {
        waterManager.PrepareNewSystem(mapOwner);
        lavaManager.PrepareNewSystem(mapOwner);
    }

    public void Clear()
    {
        waterManager.ClearFluidSystem();
        lavaManager.ClearFluidSystem();
    }

    /// <summary>
    /// Shifts all fluid elements (water and lava) up by <paramref name="count"/> grid rows.
    /// Call this after ShiftRowsUp in GarbageManager.
    /// </summary>
    public void ShiftElementsUp(int count)
    {
        waterManager.ShiftElementsUp(count);
        lavaManager.ShiftElementsUp(count);
    }

    public void OnUpdate()
    {
        waterManager.OnUpdate();
        lavaManager.OnUpdate();
        SpawnFluidConcretion();
    }

    public void ImmediateBlockSqueeze(MapManager mapOwner, Block block)
    {
        waterManager.ImmediateBlockSqueeze(mapOwner, block);
        lavaManager.ImmediateBlockSqueeze(mapOwner, block);
    }

    private void SpawnFluidConcretion()
    {
        for (int i = waterManager.fluidSystem.elements.Count - 1; i >= 0; i--)
        {
            FluidElement waterElement = waterManager.fluidSystem.elements[i];
            List<FluidElement> collidedLavaElements = lavaManager.fluidSystem.GetCollidedElements(waterElement);
            FluidElement lavaElement = collidedLavaElements.Count > 0 ? collidedLavaElements[0] : null;
            if (lavaElement != null && lavaElement.amount > 0 && waterElement.amount > 0)
            {
                int concretionAmount = Mathf.Min(waterElement.upperLevel, lavaElement.upperLevel)
                                     - Mathf.Max(waterElement.lowerLevel, lavaElement.lowerLevel);
                waterElement.amount -= concretionAmount;
                lavaElement.amount  -= concretionAmount;

                Block spawnedBlock;
                if (waterElement.hasFlown)
                    spawnedBlock = BlockSpawner.NewBlock(waterToLava);
                else if (lavaElement.hasFlown)
                    spawnedBlock = BlockSpawner.NewBlock(lavaToWater);
                else
                    spawnedBlock = BlockSpawner.NewBlock(waterToLava);

                spawnedBlock.GetComponent<BlockSoundManager>().placedSounds = new AudioClip[] { fizzSound };
                map.RequestSpawnBlock(spawnedBlock, waterElement.column, waterElement.lowerGridPosition);
            }
        }
    }
}
