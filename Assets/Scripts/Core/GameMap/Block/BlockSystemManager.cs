using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Collections.AllocatorManager;

public class BlockSystemManager : MonoBehaviour
{
    // block grid data
    [Header("Block grid")]
    [SerializeField] private BlockGridManager blockGridManager;
    [SerializeField] public Transform blockRoot;
    private IReadonlyBlockGrid blockGrid => blockGridManager.Grid;

    // map reference
    private MapManager map;

    // block grid events
    //public event Action<MapManager, Block> OnBlockGridPlaced;
    //public event Action<Block> OnBlockLockdown;

    // grid param getters
    public int GridWidth => blockGrid.GridWidth;
    public int GridHeight => blockGrid.GridHeight;

    // block grid data getters
    public IReadOnlyList<Block> Blocks => blockGridManager.RegisteredBlocks;

    // Block update system
    public BlockNCUpdateManager BlockNCUpdateManager { get; private set; }

    // Redstone system
    public RedstoneManager RedstoneManager { get; private set; }


    public void Initialise(MapManager mapOwner)
    {
        Debug.Assert(mapOwner != null);
        Debug.Assert(blockGridManager != null);
        Debug.Assert(blockRoot != null);

        map = mapOwner;

        blockGridManager.OnRegisteredBlock += SubscribeBlock;
        blockGridManager.OnUnregisteredBlock += UnsubscribeBlock;
        blockGridManager.OnGridBlockPlaced += NotifyGridPlace;
        blockGridManager.OnGridBlockRemoved += OnGridBlockRemovedHandler;
    }

    public void Dispose()
    {
        blockGridManager.OnRegisteredBlock -= SubscribeBlock;
        blockGridManager.OnUnregisteredBlock -= UnsubscribeBlock;
        blockGridManager.OnGridBlockPlaced -= NotifyGridPlace;
        blockGridManager.OnGridBlockRemoved -= OnGridBlockRemovedHandler;
    }

    public void PrepareNewMap(int width, int height)
    {
        blockGridManager.Init(map, width, height);

        BlockNCUpdateManager = new BlockNCUpdateManager(map, blockGrid);
        RedstoneManager = new RedstoneManager(map);
    }

    public void ClearMap()
    {
        blockGridManager.ClearForced();
    }

    public void OnUpdate()
    {
        // Framely Block Update
        for (int i = 0; i < Blocks.Count; i++)
        {
            if (Blocks[i] != null)
                Blocks[i].OnUpdate();
        }

        // Block pending update requests
        blockGridManager.ProcessPendingBlockRequests();

        // NC update
        BlockNCUpdateManager.BlockUpdate();

        // Redstone
        RedstoneManager.RedstoneUpdate();
    }

    public void RequestSpawnTetromino(MapTetromino tetromino)
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
                Debug.Log(new Vector2Int(x, y));

                // request spawns
                blockGridManager.RequestSpawnBlock(block, new(x, y), false);

                // parenting
                block.transform.SetParent(tetromino.transform);
            }
        Debug.Log($"Twtromino positon {tetromino.position}");
    }

    public void RequestSpawnBlock(Block block, int x, int y)
    {
        blockGridManager.RequestSpawnBlock(block, new(x, y), true);
    }

    public void RequestMoveBlock(Block block, int x, int y, bool animated)
    {
        blockGridManager.RequestMoveBlock(block, new(x, y));
    }

    public void RequestRemoveBlock(Block block)
    {
        blockGridManager.RequestRemoveBlock(block);
    }

    public void RequestDestroyBlock(Block block)
    {
        blockGridManager.RequestDestroyBlock(block);
    }

    public void ImmediatelyProcessGridPendingUpdates()
    {
        blockGridManager.ProcessPendingBlockRequests();
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
        return blockGrid.IsEmpty(x, y) || blockGrid.Get(x, y).IsDummy;
    }

    public bool CheckInsideGrid(int x, int y)
    {
        return blockGrid.CheckInsideGrid(x, y);
    }

    public bool CheckRowFull(int row)
    {
        for (int column = 0; column < GridWidth; column++)
        {
            Block block = blockGrid.Get(column, row);
            if (block == null || !block.IsClearable())
            {
                return false;
            }
        }

        return true;
    }

    public bool CheckRowEmpty(int row)
    {
        for (int column = 0; column < GridWidth; column++)
        {
            Block block = blockGrid.Get(column, row);
            if (block != null && block.isLocked)
            {
                return false;
            }
        }

        return true;
    }

    public bool CheckMapEmpty()
    {
        for (int row = 0; row < GridHeight - 1; row++)
        {
            if (!CheckRowEmpty(row))
            {
                return false;
            }
        }

        return true;
    }

    public bool Contains(Block block)
    {
        return blockGrid.Contains(block);
    }

    private void SubscribeBlock(Block block)
    {
        block.OnLockedDown += OnBlockRequestSendingNCUpdate;
        block.OnLockedDown += ReparentBlock;
        block.OnSelfNCUpdateRequest += OnBlockRequestSendingNCUpdate;
    }

    private void UnsubscribeBlock(Block block)
    {
        block.OnLockedDown -= OnBlockRequestSendingNCUpdate;
        block.OnLockedDown -= ReparentBlock;
        block.OnSelfNCUpdateRequest -= OnBlockRequestSendingNCUpdate;
    }

    public void OnBlockRequestSendingNCUpdate(Block block)
    {
        if (block == null)
            return;
        BlockNCUpdateManager.RequestPendingNCUpdateSource(block.GridPosition);
    }

    private void OnGridBlockRemovedHandler(Vector2Int pos, Block block)
    {
        BlockNCUpdateManager.RequestPendingNCUpdateSource(pos);
    }

    private void NotifyGridPlace(Vector2Int position, Block block)
    {
        map.HandleOnGridBlockPlaced(block);
    }

    private void ReparentBlock(Block block)
    {
        map.ReparentBlock(block);
    }
}
