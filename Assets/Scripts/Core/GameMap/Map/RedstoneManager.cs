using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RedstoneManager
{
    private readonly MapManager map;
    private readonly HashSet<Block> pendingUpdate = new();
    private readonly Dictionary<Block, bool> activationStates = new(); // block → wasActivated

    public RedstoneManager(MapManager map)
    {
        this.map = map;
    }

    public void RequestUpdate(Block block)
    {
        if (block == null || block.isRemoved) return;
        pendingUpdate.Add(block);
    }

    public void RedstoneUpdate()
    {
        Block[] copy = pendingUpdate.ToArray();
        pendingUpdate.Clear();

        foreach (var block in copy)
        {
            if (block == null || block.isRemoved)
            {
                activationStates.Remove(block);
                continue;
            }
            if (block is not IRedstoneActivatable activatable) continue;

            bool isNowActivated = EvaluateActivation(block, activatable);
            activationStates.TryGetValue(block, out bool wasActivated);

            if (!wasActivated && isNowActivated)
            {
                activationStates[block] = true;
                activatable.OnRedstoneActivated();
            }
            else if (wasActivated && !isNowActivated)
            {
                activationStates[block] = false;
                // IRedstoneOneShotActivatable blocks do not respond to deactivation
                if (block is not IRedstoneOneShotActivatable)
                {
                    activatable.OnRedstoneDeactivated();
                }
            }
        }
    }

    private bool EvaluateActivation(Block block, IRedstoneActivatable activatable)
    {
        foreach (var neighbor in map.GetAdjacentBlocks(block.GridPosition.x, block.GridPosition.y))
        {
            if (neighbor is IRedstonePowerSource source
                && source.PowersPosition(neighbor.GridPosition, block.GridPosition)
                && activatable.CanReceivePowerFrom(neighbor))
                return true;
        }
        return false;
    }
}
