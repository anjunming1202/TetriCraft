using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockUpdateManager
{
    public BlockUpdateManager(MapManager map)
    {
        this.map = map;
    }

    public static void OnNeighbourChangedBlockUpdate(BlockGrid grid, Vector2Int pos, Block block = null)
    {
        // NC update triggered (other notifications other than neighbours)
        Block self = grid.Get(pos.x, pos.y);
        if (self == null && block != null)
            self = block;
        if (self != null)
        {
            self.NeighbourChangedNotified(pos);
            self.OnNCUpdateTriggered();
        }

        // notify neighbours (NC notification list)
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            Vector2Int nPos = pos + dir;
            Block neighbour = grid.Get(nPos.x, nPos.y);
            if (neighbour != null)
            {
                neighbour.NeighbourChangedNotified(pos);
            }
        }
    }

    public static void SubscribeNCNotification(Block notifier, Block listener)
    {
        notifier.OnNCBlockUpdated += listener.NeighbourChangedNotified;
        if (NCSubscibeList.ContainsKey(listener))
            NCSubscibeList[listener].Add(notifier);
        else
            NCSubscibeList.Add(listener, new List<Block> { notifier });
        listener.OnRemoved += DesubscribeAllNCNotifications;
    }

    public static void DesubscribeNCNotification(Block notifier, Block listener)
    {
        notifier.OnNCBlockUpdated -= listener.NeighbourChangedNotified;
        NCSubscibeList[listener].Remove(notifier);
    }

    public static void DesubscribeAllNCNotifications(Block listener)
    {
        if (!NCSubscibeList.ContainsKey(listener))
            return;

        for (int i = NCSubscibeList[listener].Count - 1; i >= 0; i--)
        {
            Block notifier = NCSubscibeList[listener][i];
            DesubscribeNCNotification(notifier, listener);
        }
    }

    public void BlockUpdate()
    {
        (Block, Vector2Int)[] copy = blockUpdateList.ToArray();

        Reset(); // prepare update list for next frame, some updates might occur in the following activations

        foreach (var (block, updateSrc) in copy)
        {
            block.NCNotificationUpdate(updateSrc);
        }
    }

    public void AddUpdatedBlock(Block block, Vector2Int updateSource)
    {
        if (blockUpdateList.Contains((block, updateSource)) || block.isRemoved)
            return;
        blockUpdateList.Add((block, updateSource));
    }

    private MapManager map;
    private List<(Block, Vector2Int)> blockUpdateList = new List<(Block, Vector2Int)>();

    private static Dictionary<Block, List<Block>> NCSubscibeList = new Dictionary<Block, List<Block>>();

    private void Reset()
    {
        blockUpdateList.Clear();
    }
}
