using BlockSystem;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class BlockGridManager : MonoBehaviour
{
    [SerializeField] private BlockGrid blockGrid;
    private MapManager registedMap;

    // block grid update management
    private readonly HashSet<Block> registeredBlocks = new();

    private readonly Dictionary<Block, SpawnRequest> blockSpawnBatch = new();
    private readonly HashSet<Block> blockRemoveBatch = new();
    private readonly HashSet<Block> blockDestroyBatch = new();
    private readonly Dictionary<Block, Vector2Int> blockMoveBatch = new();

    public event Action<Block> OnRegisteredBlock;
    public event Action<Block> OnUnregisteredBlock;

    public event Action<Vector2Int, Block> OnBlockPlacedUpdate;

    public IReadonlyBlockGrid Grid => blockGrid;
    public IReadOnlyList<Block> RegisteredBlocks => blockGrid.Blocks;

    private struct SpawnRequest
    {
        public Block block;
        public Vector2Int spawnPosition;
        public bool lockdownImmediately;

        public SpawnRequest(Block block, Vector2Int position, bool lockdownImmediately) : this()
        {
            this.block = block;
            this.spawnPosition = position;
            this.lockdownImmediately = lockdownImmediately;
        }
    }

    private enum RequestProcessPhase
    {
        Waiting,
        PreResolvingConflicts,
        ProcessingRemove,
        ProcessingMove,
        ProcessingSpawn,
    }
    private RequestProcessPhase requestProcessPhase;

    public void Init(MapManager registedMap, int width, int height)
    {
        this.registedMap = registedMap;

        blockGrid.Init(width, height);

        requestProcessPhase = RequestProcessPhase.Waiting;

        Debug.Log("BlockGridManager reset for the game");
    }

    public void ClearForced()
    {
        foreach (Block block in registeredBlocks)
        {
            if (block != null)
                GameObject.Destroy(block.gameObject);
        }
        blockSpawnBatch.Clear();
        blockMoveBatch.Clear();
        blockRemoveBatch.Clear();
        blockDestroyBatch.Clear();
    }

    public void ProcessPendingBlockRequests()
    {
        LogRequests();
        requestProcessPhase = RequestProcessPhase.PreResolvingConflicts;
        ResolveRequestConflicts();
        ResolveOccupationConflicts();

        LogRequests();

        requestProcessPhase = RequestProcessPhase.ProcessingRemove;
        ProcessDestroyBatch();
        ProcessRemoveBatch();

        requestProcessPhase = RequestProcessPhase.ProcessingMove;
        ProcessMoveBatch();

        requestProcessPhase = RequestProcessPhase.ProcessingSpawn;
        ProcessSpawnBatch();

        ClearAllBatches();
        requestProcessPhase = RequestProcessPhase.Waiting;
    }


    public void RequestSpawnBlock(Block block, Vector2Int position, bool lockdownImmediately)
    {
        if (block == null)
            return;

        if (blockSpawnBatch.ContainsKey(block))
        {
            Debug.LogError("Request spawning an registered existing block");
            return;
        }

        // Last write wins
        blockSpawnBatch[block] = new SpawnRequest(block, position, lockdownImmediately);

        // A spawn request overrides a pending remove for the same block
        blockRemoveBatch.Remove(block);

        Debug.Assert(requestProcessPhase == RequestProcessPhase.Waiting, $"Try request spawn at {requestProcessPhase} phase");
    }

    public void RequestMoveBlock(Block block, Vector2Int position)
    {
        if (block == null)
            return;

        // Last write wins
        blockMoveBatch[block] = position;

        Debug.Assert(requestProcessPhase == RequestProcessPhase.Waiting, $"Try request move at {requestProcessPhase} phase");
    }

    public void RequestRemoveBlock(Block block)
    {
        if (block == null)
            return;

        // Destroy has higher priority than remove.
        if (blockDestroyBatch.Contains(block))
            return;

        // if request occurs during processing, immediately apply it
        if (requestProcessPhase == RequestProcessPhase.ProcessingRemove || 
            requestProcessPhase == RequestProcessPhase.ProcessingSpawn || 
            requestProcessPhase == RequestProcessPhase.ProcessingMove)
        {
            ApplyRemove(block);
            return;
        }

        blockRemoveBatch.Add(block);

        Debug.Assert(requestProcessPhase == RequestProcessPhase.Waiting, $"Try request remove at {requestProcessPhase} phase");
    }

    public void RequestDestroyBlock(Block block)
    {
        if (block == null)
            return;

        // if request occurs during processing, immediately apply it
        if (requestProcessPhase == RequestProcessPhase.ProcessingRemove ||
            requestProcessPhase == RequestProcessPhase.ProcessingSpawn ||
            requestProcessPhase == RequestProcessPhase.ProcessingMove)
        {
            ApplyDestroy(block);
            return;
        }

        blockDestroyBatch.Add(block);
        blockRemoveBatch.Remove(block);

        Debug.Assert(requestProcessPhase == RequestProcessPhase.Waiting, $"Try request destroy at {requestProcessPhase} phase");
    }

    private void ProcessDestroyBatch()
    {
        if (blockDestroyBatch.Count == 0)
            return;

        foreach (Block block in blockDestroyBatch)
        {
            ApplyDestroy(block);
        }
    }

    private void ProcessRemoveBatch()
    {
        if (blockRemoveBatch.Count == 0)
            return;

        foreach (Block block in blockRemoveBatch)
        {
            ApplyRemove(block);
        }
    }

    private void ProcessSpawnBatch()
    {
        if (blockSpawnBatch.Count == 0)
            return;

        foreach (var (_, request) in blockSpawnBatch)
        {
            Block block = request.block;
            Vector2Int position = request.spawnPosition;
            bool lockdownImmediately = request.lockdownImmediately;

            ApplySpawn(block, position, lockdownImmediately);
        }
    }

    private void ProcessMoveBatch()
    {
        if (blockMoveBatch.Count == 0)
            return;

        ApplyMoves(blockMoveBatch);
    }

    private void RegisterBlock(Block block)
    {
        if (block == null)
            return;

        if (!registeredBlocks.Add(block))
            return;

        OnRegisteredBlock?.Invoke(block);

        block.OnRegistered(registedMap);
    }

    private void UnregisterBlock(Block block)
    {
        if (block == null)
            return;

        if (!registeredBlocks.Remove(block))
            return;

        OnUnregisteredBlock?.Invoke(block);

        block.OnUnregistered();
    }

    private void ApplySpawn(Block block, Vector2Int position, bool lockdownImmediately)
    {
        if (block == null)
            return;

        // additional check making sure the spawn is valid
        if (!CheckCanOccupy(block, position))
        {
            Debug.LogError($"The position {position} cannot be occupied by {block} when spawned");
            return;
        }

        // Now occupy the cell
        OccupyWithReplacement(block, position);

        // Successful spawn
        // initialise block
        RegisterBlock(block);

        block.SetGridPosition(position, false);

        block.OnPostSpawned();

        if (lockdownImmediately)
            block.OnLockdown();

        OnBlockPlacedUpdate?.Invoke(position, block);

        //Debug.Log($"{block} spawned at {position}, block position {block.Position}");
    }

    private void ApplyMoves(Dictionary<Block, Vector2Int> moves)
    {
        // remove moving blocks
        Dictionary<Block, Vector2Int> successfulRemoves = new();
        foreach (var (block, targetPosition) in moves)
        {
            if (block == null)
                continue;

            // designed for failed remove
            Vector2Int lastPosition = blockGrid.GetPosition(block);

            if (blockGrid.TryRemove(block))
                successfulRemoves.Add(block, lastPosition);
            else
            {
                foreach (var (removedBlock, removedPosition) in successfulRemoves)
                {
                    blockGrid.TryAdd(removedBlock, removedPosition);
                }
                Debug.LogError($"Failed to pre-remove {block} at {targetPosition} when applying moves!");
                return;
            }
        }

        // additional check making sure the moves are valid
        foreach (var (block, targetPosition) in moves)
        {
            if (block == null)
                continue;

            if (!CheckCanOccupy(block, targetPosition))
            {
                Debug.LogError($"The position {targetPosition} cannot be occupied by {block} when moved");
                continue;
            }
        }

        // add moved blocks
        HashSet<Block> successfulAdds = new();
        foreach (var (block, targetPosition) in moves)
        {
            if (block == null)
                continue;

            OccupyWithReplacement(block, targetPosition);

            // originally designed for failed add, now is unesseccary 
            if (CheckCanOccupy(block, targetPosition))
                successfulAdds.Add(block);
            else
            {
                foreach (var addedBlock in successfulAdds)
                {
                    blockGrid.TryRemove(addedBlock);
                }
                foreach (var (removedBlock, removedPosition) in successfulRemoves)
                {
                    blockGrid.TryAdd(removedBlock, removedPosition);
                }
                Debug.LogError($"Failed to add back {block} at {targetPosition} when applying moves!");
                return;
            }
        }

        foreach (var (block, targetPosition) in moves)
        {
            if (block == null)
                continue;

            block.SetGridPosition(targetPosition, true);
            block.OnPostMoved();

            OnBlockPlacedUpdate?.Invoke(targetPosition, block);

            //Debug.Log($"{block} moved to {targetPosition}, block position {block.Position}");
        }
    }

    private void ApplyDestroy(Block block)
    {
        if (block == null)
            return;

        // successful destroy
        blockGrid.TryRemove(block);
        UnregisterBlock(block);
        block.OnPostDestroyed();

        // destroy
        GameObject.Destroy(block.gameObject);
    }

    private void ApplyRemove(Block block)
    {
        if (block == null)
            return;

        // successful remove
        blockGrid.TryRemove(block);
        UnregisterBlock(block);
        block.OnPostRemoved();

        // destroy
        GameObject.Destroy(block.gameObject);
    }

    /// <summary>
    /// resolve conflicts before processing
    /// </summary>
    private void ResolveRequestConflicts()
    {
        // destroy:remove => destroy
        foreach (Block block in blockDestroyBatch)
        {
            bool conflict = blockRemoveBatch.Remove(block);
        }

        // spawn:move => spawn
        foreach (Block block in blockSpawnBatch.Keys)
        {
            bool conflict = blockMoveBatch.Remove(block);
        }

        // move:destroy/remove => destroy/remove
        // spawn:destroy/remove => reject all
        List<Block> rejectedBlocks = new List<Block>();
        foreach (Block block in blockDestroyBatch)
        {
            bool conflict = blockMoveBatch.Remove(block);

            if (blockSpawnBatch.Remove(block))
                rejectedBlocks.Add(block);
        }
        foreach (Block block in blockRemoveBatch)
        {
            bool conflict = blockMoveBatch.Remove(block);

            if (blockSpawnBatch.Remove(block))
                rejectedBlocks.Add(block);
        }
        foreach(Block block in rejectedBlocks)
        {
            blockDestroyBatch.Remove(block);
            blockRemoveBatch.Remove(block);
        }
    }

    private void ResolveOccupationConflicts()
    {
        HashSet<Block> rejectedBlocks = new();

        Dictionary<Vector2Int, Block> occupations = new();

        // Check spawn prior to move
        foreach (var (block, spawnRequest) in blockSpawnBatch)
        {
            Block finalBlock = block;
            Vector2Int position = spawnRequest.spawnPosition;

            // check map block replacement
            if (!blockGrid.IsEmpty(position))
            {
                Block existing = blockGrid.Get(position);
                bool willLeave = blockMoveBatch.ContainsKey(existing) || blockRemoveBatch.Contains(existing) || blockDestroyBatch.Contains(existing);
                if (!willLeave)
                {
                    // new spawn can replace existing block => allow spawn (replacement at applying spawn)
                    if (CheckCanOccupy(block, position))
                    {
                        rejectedBlocks.Add(existing);
                    }
                    // fail to spawn (has not been registered, remove/destroy directly)
                    else
                    {
                        finalBlock = existing;
                        rejectedBlocks.Add(block);

                        SpawnFailureDisposition spawnFailureDisposition = block.GetSpawnFailureDisposition();
                        switch (spawnFailureDisposition)
                        {
                            case SpawnFailureDisposition.None:
                                // invalid spawn
                                block.OnPostSpawnFailed(registedMap, position);
                                GameObject.Destroy(block.gameObject);
                                //Debug.LogError($"The block {block} spawn request at {position} is invalid, as the position is occupied by block {existing} in the grid with no request of move/remove");
                                break;
                            case SpawnFailureDisposition.Remove:
                                block.OnPostSpawnFailed(registedMap, position);
                                block.OnPostRemoved();
                                GameObject.Destroy(block.gameObject);
                                break;
                            case SpawnFailureDisposition.Destroy:
                                block.OnPostSpawnFailed(registedMap, position);
                                block.OnPostDestroyed();
                                GameObject.Destroy(block.gameObject);
                                break;
                        }
                    }
                }
            }
            // check prior spawn
            if (occupations.TryGetValue(position, out var existingSpawn))
            {
                // new spawn can replace another previous spawn => allow the latest spawn
                if (CheckCanOccupy(block, position))
                {
                    rejectedBlocks.Add(existingSpawn);

                    // last spawn is failed because of multiple requests
                    SpawnFailureDisposition spawnFailureDisposition = existingSpawn.GetSpawnFailureDisposition();
                    switch (spawnFailureDisposition)
                    {
                        case SpawnFailureDisposition.None:
                            // just reject failed block
                            existingSpawn.OnPostSpawnFailed(registedMap, position);
                            GameObject.Destroy(existingSpawn.gameObject);
                            break;
                        case SpawnFailureDisposition.Remove:
                            existingSpawn.OnPostSpawnFailed(registedMap, position);
                            existingSpawn.OnPostRemoved();
                            GameObject.Destroy(existingSpawn.gameObject);
                            break;
                        case SpawnFailureDisposition.Destroy:
                            existingSpawn.OnPostSpawnFailed(registedMap, position);
                            existingSpawn.OnPostDestroyed();
                            GameObject.Destroy(existingSpawn.gameObject);
                            break;
                    }
                }
                // fail to spawn (has not been registered, remove/destroy directly)
                else
                {
                    finalBlock = existingSpawn;
                    rejectedBlocks.Add(block);

                    SpawnFailureDisposition spawnFailureDisposition = block.GetSpawnFailureDisposition();
                    switch (spawnFailureDisposition)
                    {
                        case SpawnFailureDisposition.None:
                            // invalid spawn
                            block.OnPostSpawnFailed(registedMap, position);
                            GameObject.Destroy(block.gameObject);
                            //Debug.LogError($"The spawn request at {position} is invalid, as two blocks {block} and {existingSpawn} are trying to spawn at the same position with no replacement");
                            break;
                        case SpawnFailureDisposition.Remove:
                            block.OnPostSpawnFailed(registedMap, position);
                            block.OnPostRemoved();
                            GameObject.Destroy(block.gameObject);
                            break;
                        case SpawnFailureDisposition.Destroy:
                            block.OnPostSpawnFailed(registedMap, position);
                            block.OnPostDestroyed();
                            GameObject.Destroy(block.gameObject);
                            break;
                    }
                }
            }

            occupations[position] = finalBlock;
        }
        
        // Check move
        foreach (var (block, position) in blockMoveBatch)
        {
            Block finalBlock = block;

            // check map block replacement
            if (!blockGrid.IsEmpty(position))
            {
                Block existing = blockGrid.Get(position);
                bool willLeave = blockMoveBatch.ContainsKey(existing) || blockRemoveBatch.Contains(existing) || blockDestroyBatch.Contains(existing);
                if (!willLeave)
                {
                    if (CheckCanOccupy(block, position))
                    {
                        rejectedBlocks.Add(existing);
                    }
                    else
                    {
                        finalBlock = existing;
                        rejectedBlocks.Add(block);
                        Debug.LogError($"The block {block} move request to {position} is invalid, as the position is occupied by block {existing} in the grid with no request of move/remove");
                    }
                }
            }
            // check prior spawn or move
            if (occupations.TryGetValue(position, out var existingSpawnOrMove))
            {
                if (CheckCanOccupy(block, position))
                {
                    rejectedBlocks.Add(existingSpawnOrMove);
                }
                else
                {
                    finalBlock = existingSpawnOrMove;
                    rejectedBlocks.Add(block);
                    bool isExistingMove = blockMoveBatch.ContainsKey(existingSpawnOrMove);
                    Debug.LogError($"The block {block} move request to {position} is invalid, as the target position is occupied by another block {(isExistingMove ? "move" : "spawn")} request with no replacement");
                }
            }

            occupations[position] = finalBlock;
        }

        // Reject requests
        foreach (Block block in rejectedBlocks)
        {
            if (blockSpawnBatch.ContainsKey(block))
                Debug.LogWarning($"block {block} spawn request rejected");
            if (blockMoveBatch.ContainsKey(block))
                Debug.LogWarning($"block {block} move request rejected");
            blockSpawnBatch.Remove(block);
            blockMoveBatch.Remove(block);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>whether replacement is allowed</returns>
    private bool CheckCanOccupy(Block incoming, Vector2Int position)
    {
        if (!blockGrid.TryGet(position, out Block existing))
            return true;

        if (existing == incoming)
            return true;

        if (!existing.CanBeReplacedBy(incoming))
            return false;

        return true;
    }

    private void OccupyWithReplacement(Block incoming, Vector2Int position)
    {
        blockGrid.TryGet(position, out var existing);

        //if (existing == incoming)

        if (existing == null)
        {
            blockGrid.TryAdd(incoming, position);
        }
        else
        {
            // handling block being replaced
            ReplacementDisposition replacementDisposition = existing.GetReplacementDisposition(incoming);
            switch (replacementDisposition)
            {
                case ReplacementDisposition.Disallow:
                    Debug.LogError($"{existing} should not be replaced by {incoming}");
                    break;
                case ReplacementDisposition.Remove:
                    ApplyRemove(existing);
                    break;
                case ReplacementDisposition.Destroy:
                    ApplyDestroy(existing);
                    break;
            }

            blockGrid.TryAdd(incoming, position);

            existing.OnReplacedBy(incoming);
        }
    }

    private void ClearAllBatches()
    {
        blockSpawnBatch.Clear();
        blockRemoveBatch.Clear();
        blockDestroyBatch.Clear();
        blockMoveBatch.Clear();
    }

    private void LogRequests()
    {
        if (blockSpawnBatch.Count == 0 && blockMoveBatch.Count == 0 && blockDestroyBatch.Count == 0 && blockRemoveBatch.Count == 0)
            return;
        Debug.Log($"Current Phase: {requestProcessPhase}");
        Debug.Log($"Spawn Requests: {blockSpawnBatch.Count}");
        Debug.Log($"Move Requests: {blockMoveBatch.Count}");
        Debug.Log($"Destroy Requests: {blockDestroyBatch.Count}");
        Debug.Log($"Remove Requests: {blockRemoveBatch.Count}");
        Debug.Log($"Total Registered Blocks: {registeredBlocks.Count}");
    }
}
