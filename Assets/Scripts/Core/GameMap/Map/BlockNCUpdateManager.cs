using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class BlockNCUpdateManager
{
    private MapManager map;
    private IReadonlyBlockGrid blockGrid;
    private readonly List<(Block, Vector2Int)> blockNCUpdateRecieverList = new List<(Block, Vector2Int)>();

    private static readonly Dictionary<Block, List<Block>> NCListenerList = new(); // { notifier : { listeners }}
    private static readonly Dictionary<Block, List<Block>> NCNotifierList = new(); // { listener : { notifiers }}

    public BlockNCUpdateManager(MapManager map, IReadonlyBlockGrid blockGrid)
    {
        this.map = map;
        this.blockGrid = blockGrid;
    }

    private void Reset()
    {
        blockNCUpdateRecieverList.Clear();
    }

    /// <summary>
    /// Send an NC update to the neighbour receivers
    /// </summary>
    public void SendNCUpdateRequestToNeighbours(Vector2Int pos)
    {
        // NC update triggered (other notifications other than neighbours)
        /*Block self = grid.Get(pos.x, pos.y);
        if (self == null && block != null)
            self = block;
        if (self != null)
        {
            self.ReceiveNCUpdateRequest(pos);
            self.SendNCUpdate();
        }*/

        // notify neighbours (NC notification list)
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            Vector2Int nPos = pos + dir;
            Block neighbour = blockGrid.Get(nPos.x, nPos.y);
            if (neighbour != null)
            {
                AddToNCUpdateBatch(neighbour, pos);
            }
        }
    }

    public void SendNCUpdateRequestToExtraReceivers(Block notifier)
    {
        if (notifier ==  null)
            return;

        if (!NCListenerList.TryGetValue(notifier, out var listeners))
            return;

        foreach (Block listener in listeners)
        {
            if (listener != null)
            {
                AddToNCUpdateBatch(listener, notifier.GridPosition);
            }
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

    public void BlockUpdate()
    {
        (Block, Vector2Int)[] copy = blockNCUpdateRecieverList.ToArray();

        Reset(); // prepare update list for next frame, some updates might occur in the following activations

        foreach (var (block, updateSrc) in copy)
        {
            if (block != null)
                block.OnNCUpdateRespond(updateSrc);
        }
    }

    public void AddToNCUpdateBatch(Block receiverBlock, Vector2Int updateSource)
    {
        if (blockNCUpdateRecieverList.Contains((receiverBlock, updateSource)) || receiverBlock.isRemoved)
            return;
        blockNCUpdateRecieverList.Add((receiverBlock, updateSource));
    }
}
