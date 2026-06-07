using System;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

public class BlockGridManager : MonoBehaviour
{
    // block grid data
    [Header("Block grid")]
    [SerializeField] private BlockGrid blockGrid;
    [SerializeField] public Transform blockRoot;

    // map reference
    private MapManager map;

    // block grid params
    private int boundaryWidth;
    private int boundaryHeight;

    // block grid update management
    private List<Block> blockList;
    private List<Block> blockUpdateBatch;
    private List<Block> blockDestroyBatch;

    // block grid events
    public event Action<MapManager, Block> OnGridPlace;

    // grid param getters
    public int BoundaryWidth => boundaryWidth;
    public int BoundaryHeight => boundaryHeight;

    // block grid data getters
    public BlockGrid Grid => blockGrid;
    public Block this[int x, int y] => blockGrid[x, y];
    public int BlockCount => blockGrid != null ? blockGrid.blockCount : 0;

    // block grid update management getters
    public IReadOnlyList<Block> Blocks => blockList;
    public IReadOnlyList<Block> BatchBlocks => blockUpdateBatch;

    // Block update system
    public BlockUpdateManager BlockUpdateManager { get; private set; }

    // Redstone system
    public RedstoneManager RedstoneManager { get; private set; }


    public void Initialise(MapManager mapOwner)
    {
        Debug.Assert(mapOwner != null);
        Debug.Assert(blockGrid != null);
        Debug.Assert(blockRoot != null);

        map = mapOwner;
    }


    public void PrepareNewMap(int width, int height)
    {
        boundaryWidth = width;
        boundaryHeight = height;

        blockGrid.Init(width, height + 5);

        blockList = new List<Block>();
        blockUpdateBatch = new List<Block>();

        BlockUpdateManager = new BlockUpdateManager(map, blockGrid);
        RedstoneManager = new RedstoneManager(map);
    }

    public void ClearMap()
    {
        foreach (var block in blockList)
            if (block != null)
                GameObject.Destroy(block.gameObject);

        if (blockGrid != null)
        {
            blockGrid.Clear();
        }

        blockList = new List<Block>();
        blockUpdateBatch = new List<Block>();
    }

    public void OnUpdate()
    {
        // Framely block update
        for (int i = 0; i < blockList.Count; i++)
        {
            if (blockList[i] != null)
                blockList[i].OnUpdate();
        }

        // Deffered block grid update
        if (blockUpdateBatch.Count > 0)
        {
            BatchUpdateBlocks();
        }

        // Block update
        BlockUpdateManager.BlockUpdate();

        // Redstone
        RedstoneManager.RedstoneUpdate();
    }

    public void SpawnTetromino(MapTetromino tetromino)
    {
        for (int r = 0; r < tetromino.size; r++)
            for (int c = 0; c < tetromino.size; c++)
            {
                // get block from the tetromino
                Block block = tetromino.shape[r, c];
                if (block == null)
                    continue;
                Vector2Int gridPosition = tetromino.LocalToMap(r, c);
                int x = gridPosition.x;
                int y = gridPosition.y;

                // register to the block grid system
                RegisterBlock(block, x, y);

                // try add to grid data
                if (!TryOccupyCell(block, x, y))
                {
                    UnregisterBlock(block);
                    Debug.LogError($"Fail to spawn block {block}");
                    return;
                }

                // parenting
                block.transform.SetParent(tetromino.transform);
            }
    }

    public void SpawnBlock(Block block, int x, int y)
    {
        // register to the block grid system
        RegisterBlock(block, x, y);

        // try add into grid data
        if (!TryOccupyCell(block, x, y))
        {
            UnregisterBlock(block);
            Debug.LogError($"Fail to spawn block {block}");
            return;
        }

        // block lifecycle action
        block.OnLockdown();
        
        // parenting
        block.transform.SetParent(blockRoot);
    }

    public void DestroyBlock(Block block)
    {
        // release from grid data
        TryReleaseCell(block);

        // unregister from the system
        UnregisterBlock(block);

        // block lifecycle action
        block.Destroyed();

        // destroy
        GameObject.Destroy(block.gameObject);
    }

    public void RemoveBlock(Block block)
    {
        // release grid data
        TryReleaseCell(block);

        // unregister from the system
        UnregisterBlock(block);

        // block lifecycle action
        block.Removed();

        // destroy
        GameObject.Destroy(block.gameObject);
    }

    public void BatchUpdateBlocks()
    {
        // Remove all moving blocks from the grid first.
        foreach (Block block in blockUpdateBatch)
        {
            TryReleaseCell(block);
        }

        // Reinsert them at their new positions.
        foreach (Block block in blockUpdateBatch)
        {
            int x = block.GridPosition.x;
            int y = block.GridPosition.y;

            // dummy block => replaced ***
            if (blockGrid[x, y] != null && blockGrid[x, y].IsDummy)
            {
                RemoveBlock(blockGrid[x, y]);
            } // TODO

            blockGrid.TryAdd(block);
        }

        // Batched placed events
        foreach (Block block in blockUpdateBatch)
        {
            NotifyGridPlace(block);
        }

        blockUpdateBatch.Clear();
    }

    public Block GetBlock(int x, int y)
    {
        return blockGrid.Get(x, y);
    }

    public IEnumerable<Block> GetAdjacentBlocks(int x, int y, bool includeSelf = false)
    {
        if (includeSelf)
        {
            yield return GetBlock(x, y);
        }

        foreach (Vector2Int offset in new Vector2Int[]
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        })
        {
            yield return GetBlock(x + offset.x, y + offset.y);
        }
    }

    public bool CheckInsideWithoudCeiling(int x, int y)
    {
        return blockGrid.CheckInsideWithoudCeiling(x, y);
    }

    public bool CheckEmpty(int x, int y)
    {
        return blockGrid.CheckEmpty(x, y) || blockGrid[x, y].IsDummy;
    }

    public bool CheckInsideGrid(int x, int y)
    {
        return blockGrid.CheckInsideGrid(x, y);
    }

    public bool CheckRowFull(int row)
    {
        for (int column = 0; column < boundaryWidth; column++)
        {
            Block block = blockGrid[column, row];
            if (block == null || !block.IsClearable())
            {
                return false;
            }
        }

        return true;
    }

    public bool CheckRowEmpty(int row)
    {
        for (int column = 0; column < boundaryWidth; column++)
        {
            Block block = blockGrid[column, row];
            if (block != null && block.isLocked)
            {
                return false;
            }
        }

        return true;
    }

    public bool CheckMapEmpty()
    {
        for (int row = 0; row < boundaryHeight - 1; row++)
        {
            if (!CheckRowEmpty(row))
            {
                return false;
            }
        }

        return true;
    }

    public void OnBlockNCUpdate(Block block)
    {
        if (block == null)
            return;
        BlockUpdateManager.SendNCUpdateRequestToNeighbours(block.GridPosition);
        BlockUpdateManager.SendNCUpdateRequestToExtraReceivers(block);
    }



    private void RegisterBlock(Block block, int x, int y)
    {
        block.OnSpawn(map, x, y);

        blockList.Add(block);

        block.OnMoved += AddToUpdateBatch;
        block.OnRemoved += RemoveFromUpdateBatch;

        block.OnLockedDown += NotifyGridPlace;
        block.OnLockedDown += OnBlockNCUpdate;
    }

    private void UnregisterBlock(Block block)
    {
        blockList.Remove(block);

        block.OnMoved -= AddToUpdateBatch;
        block.OnRemoved -= RemoveFromUpdateBatch;

        block.OnLockedDown -= NotifyGridPlace;
        block.OnLockedDown -= OnBlockNCUpdate;
        block.OnDespawned();
    }

    private bool TryOccupyCell(Block block, int x, int y, bool allowTryReplace = true)
    {
        Debug.Assert(block != null);

        // Cell is empty
        Block existing = blockGrid.Get(x, y);
        if (existing == null)
        {
            return blockGrid.TryAdd(block);
        }

        // Cannot replace
        if (!allowTryReplace)
        {
            return false;
        }

        // Existing block refuses replacement
        if (!existing.CanBeReplacedBy(block))
        {
            return false;
        }

        // Notify existing block
        existing.OnReplacedBy(block);

        // Remove old block
        RemoveBlock(existing);

        // Occupy cell with new block
        return blockGrid.TryAdd(block);
    }

    private bool TryReleaseCell(Block block)
    {
        return blockGrid.TryRemove(block);
    }

    private void AddToUpdateBatch(Block block)
    {
        if (block == null || block.isRemoved)
        {
            return;
        }

        if (!blockUpdateBatch.Contains(block))
        {
            blockUpdateBatch.Add(block);
        }
    }

    private void RemoveFromUpdateBatch(Block block)
    {
        if (block == null)
        {
            return;
        }

        if (blockUpdateBatch.Contains(block))
        {
            blockUpdateBatch.Remove(block);
        }
    }

    private void NotifyGridPlace(Block block)
    {
        OnGridPlace?.Invoke(map, block);
    }
}
