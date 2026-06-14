using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockNCUpdateManager
{
    private MapManager map;
    private IReadonlyBlockGrid blockGrid;
    private readonly List<(Block, Vector2Int)> blockNCUpdateReceiverList = new List<(Block, Vector2Int)>();
    private readonly HashSet<Vector2Int> pendingNCUpdateSources = new();

    private static readonly Dictionary<Block, List<Block>> NCListenerList = new(); // { notifier : { listeners }}
    private static readonly Dictionary<Block, List<Block>> NCNotifierList = new(); // { listener : { notifiers }}

    // Debug snapshots (previous frame)
    private HashSet<Vector2Int> debugLastSources = new();
    private List<Vector2Int> debugLastReceiverPositions = new();

    public IReadOnlyCollection<Vector2Int> DebugLastSources => debugLastSources;
    public IReadOnlyList<Vector2Int> DebugLastReceiverPositions => debugLastReceiverPositions;
    public float DebugLastSnapshotTime { get; private set; } = -1f;

    public BlockNCUpdateManager(MapManager map, IReadonlyBlockGrid blockGrid)
    {
        this.map = map;
        this.blockGrid = blockGrid;
    }

    public void RequestPendingNCUpdateSource(Vector2Int pos)
    {
        pendingNCUpdateSources.Add(pos);
    }

    public void BlockUpdate()
    {
        // Snapshot sources before clearing (only update timestamp when there is actual data)
        debugLastSources = new HashSet<Vector2Int>(pendingNCUpdateSources);
        if (pendingNCUpdateSources.Count > 0)
            DebugLastSnapshotTime = UnityEngine.Time.realtimeSinceStartup;

        // Phase 1: expand sources → receivers
        foreach (var pos in pendingNCUpdateSources)
        {
            SendNCUpdateRequestToNeighbours(pos);
            SendNCUpdateRequestToExtraReceivers(blockGrid.Get(pos.x, pos.y));
        }
        pendingNCUpdateSources.Clear();

        // Snapshot receivers before processing and clearing
        debugLastReceiverPositions.Clear();
        foreach (var (block, _) in blockNCUpdateReceiverList)
            if (block != null)
                debugLastReceiverPositions.Add(block.GridPosition);

        // Phase 2: process receivers
        (Block, Vector2Int)[] copy = blockNCUpdateReceiverList.ToArray();
        ClearReceiverList();
        foreach (var (block, updateSrc) in copy)
        {
            if (block != null)
                block.OnNCUpdateRespond(updateSrc);
        }
    }

    public static void SubscribeNCNotification(Block notifier, Block listener)
    {
        if (NCListenerList.ContainsKey(notifier))
            NCListenerList[notifier].Add(listener);
        else
            NCListenerList.Add(notifier, new List<Block> { listener });

        if (NCNotifierList.ContainsKey(listener))
            NCNotifierList[listener].Add(notifier);
        else
            NCNotifierList.Add(listener, new List<Block> { notifier });

        listener.OnAfterRemoved += DesubscribeAllNCNotifications;
    }

    public static void DesubscribeNCNotification(Block notifier, Block listener)
    {
        NCListenerList[notifier].Remove(listener);
        NCNotifierList[listener].Remove(notifier);
    }

    public static void DesubscribeAllNCNotifications(Block listener)
    {
        if (!NCNotifierList.ContainsKey(listener))
            return;

        for (int i = NCNotifierList[listener].Count - 1; i >= 0; i--)
        {
            Block notifier = NCNotifierList[listener][i];
            DesubscribeNCNotification(notifier, listener);
        }
    }

    private void ClearReceiverList()
    {
        blockNCUpdateReceiverList.Clear();
    }

    /// <summary>
    /// Send an NC update to the source block itself and its four neighbours (mirrors Minecraft block update behavior).
    /// </summary>
    private void SendNCUpdateRequestToNeighbours(Vector2Int pos)
    {
        // Also notify the block at the source position itself
        Block self = blockGrid.Get(pos.x, pos.y);
        if (self != null)
            AddToNCUpdateReceiverBatch(self, pos);

        // Notify the four neighbours
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            Vector2Int nPos = pos + dir;
            Block neighbour = blockGrid.Get(nPos.x, nPos.y);
            if (neighbour != null)
                AddToNCUpdateReceiverBatch(neighbour, pos);
        }
    }

    private void SendNCUpdateRequestToExtraReceivers(Block notifier)
    {
        if (notifier ==  null)
            return;

        if (!NCListenerList.TryGetValue(notifier, out var listeners))
            return;

        foreach (Block listener in listeners)
        {
            if (listener != null)
            {
                AddToNCUpdateReceiverBatch(listener, notifier.GridPosition);
            }
        }
    }

    private void AddToNCUpdateReceiverBatch(Block receiverBlock, Vector2Int updateSource)
    {
        if (blockNCUpdateReceiverList.Contains((receiverBlock, updateSource)) || receiverBlock.isRemoved)
            return;
        blockNCUpdateReceiverList.Add((receiverBlock, updateSource));
    }
}
