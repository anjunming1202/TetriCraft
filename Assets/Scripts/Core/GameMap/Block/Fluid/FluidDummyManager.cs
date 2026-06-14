using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FluidDummyManager : MonoBehaviour
{
    private FluidManager fluidManager;
    private MapManager mapManager;
    private BlockID dummyID;

    private readonly Dictionary<Vector2Int, FluidDummy> spawnedDummyBlocks = new();
    public IReadOnlyDictionary<Vector2Int, FluidDummy> SpawnedDummyBlocks => spawnedDummyBlocks;

    public void Init(FluidManager fm, MapManager map, BlockID dummy)
    {
        fluidManager = fm;
        mapManager = map;
        dummyID = dummy;
    }

    public void Clear()
    {
        spawnedDummyBlocks.Clear();
    }

    public void GenerateDummyBlocks(FluidSystem fluidSystem)
    {
        Dictionary<Vector2Int, FluidElement> positions = fluidSystem.CalculateBlockPositions();
        Dictionary<Vector2Int, FluidDummy> invalidDummyBlocks = new();
        invalidDummyBlocks.AddRange(spawnedDummyBlocks);

        foreach (var (position, element) in positions)
        {
            if (!mapManager.CheckInsideBlockGrid(position.x, position.y))
            {
                continue;
            }

            // if the position has been a dummy block in the map => update source element
            if (mapManager.GetBlock(position) is FluidDummy dummyBlock)
            {
                // if it is fluid dummy but not in the spawned dummy list => not the same fluid system
                if (!spawnedDummyBlocks.ContainsKey(position))
                {
                    continue;
                }

                dummyBlock.SetSourceElement(element);

                // for updating recorded blocks collection
                invalidDummyBlocks.Remove(position);

                // reupdate dummy block when fluid touching floor/ceiling
                if (element.localLowerLevel == 0 || element.localUpperLevel == 0)
                    mapManager.HandleOnGridBlockPlaced(dummyBlock);
            }
            // if the position is a new position of dummy blocks
            else
            {
                if (mapManager.GetBlock(position) != null && mapManager.GetBlock(position).IsFluid)
                    continue;

                PendingSpawnDummyBlock(position, element);
            }
        }

        // remove other currently invalid dummy blocks
        foreach (var (position, block) in invalidDummyBlocks)
        {
            PendingRemoveDummyBlock(position);
        }
    }

    private void PendingSpawnDummyBlock(Vector2Int position, FluidElement sourceElement)
    {
        //Debug.Assert(!mapManager.IsBlockedWithoutCeiling(position.x, position.y), $"fail to spawn dummy fluid block, {position} occupied");
        if (mapManager.IsBlockedWithoutCeiling(position.x, position.y))
            return;

        Block newBlock = BlockSpawner.NewBlock(dummyID);
        FluidDummy newDummyBlock = newBlock as FluidDummy;
        newDummyBlock.Init(fluidManager, sourceElement);

        mapManager.RequestSpawnBlock(newDummyBlock, position.x, position.y);

        newDummyBlock.OnSpawned += RegisterDummyBlock;
        newDummyBlock.OnAfterRemoved += UnregisterDummyBlock;
    }

    private void PendingRemoveDummyBlock(Vector2Int position)
    {
        if (mapManager.GetBlock(position) is FluidDummy)
        {
            mapManager.RequestRemoveBlock(mapManager.GetBlock(position));
        }
    }

    private void RegisterDummyBlock(Block block)
    {
        //Debug.Assert(block is FluidDummy dummy && !spawnedDummyBlocks.ContainsValue(dummy));

        if (spawnedDummyBlocks.ContainsKey(block.GridPosition))
            Debug.LogWarning(spawnedDummyBlocks);

        spawnedDummyBlocks.Add(block.GridPosition, block as FluidDummy);
    }

    private void UnregisterDummyBlock(Block block)
    {
        //Debug.Assert(block is FluidDummy dummy && spawnedDummyBlocks.ContainsValue(dummy));

        Debug.Assert(spawnedDummyBlocks.Remove(block.GridPosition), $"{block}, {block.GridPosition}, {mapManager.GetBlock(block.GridPosition)}, {block == mapManager.GetBlock(block.GridPosition)}");
    }
}
