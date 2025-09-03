using System.Collections.Generic;
using UnityEngine;

public class RedstoneManager
{ 
    public RedstoneManager(MapManager map)
    {
        this.map = map;
    }

    public void RedstoneUpdate()
    {
        Block[] copy = updatedBlocks.ToArray();

        Reset(); // prepare update list for next frame, some updates might occur in the following activations

        foreach (var block in copy)
        {
            if (block is IRedstoneOneShotActivatable)
            {

            }
            else if (block is IRedstoneActivatable component)
            {
                if (block.isActivated && !block.onActivation)
                {
                    block.isActivated = false;
                    component.OnRedstoneDeactivated();
                }
                if (!block.isActivated && block.onActivation)
                {
                    block.isActivated = true;
                    component.OnRedstoneActivated();
                }
            }
            else
            {
                Debug.LogError("redstone updating an unactivatable object");
            }
        }
    }

    public void AddUpdatedBlock(Block block)
    {
        if (updatedBlocks.Contains(block) || block.isRemoved)
            return;
        updatedBlocks.Add(block);
    }

    private MapManager map;
    private List<Block> updatedBlocks = new List<Block>();

    private void Reset()
    {
        updatedBlocks.Clear();
    }
}
