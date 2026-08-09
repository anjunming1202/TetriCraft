using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "NewBlockList", menuName = "SpawnableBlockList")]
public class SpawnableBlockList : ScriptableObject
{
    public void Add(BlockID blockID, float weight = 1)
    {
        list.Add(new BlockEntry(blockID, weight));
    }

    public void Remove(BlockID blockID)
    {
        list.RemoveAll(entry => entry.blockID == blockID);
    }

    public BlockID GetBlockID(float value)
    {
        float cumulativeWeight = 0;
        foreach (BlockEntry entry in list)
        {
            cumulativeWeight += entry.weight;
            if (cumulativeWeight > value)
            {
                return entry.blockID;
            }
        }

        Debug.LogError("Can't get a selectable random blockID!");
        return BlockID.Missing;
    }

    public float TotalWeight
    {
        get
        {
            float totalWeigth = 0;
            foreach (BlockEntry entry in list)
            {
                totalWeigth += entry.weight;
            }
            return totalWeigth;
        }
    }

    [Serializable]
    private struct BlockEntry
    {
        public BlockID blockID;
        public float weight;
        public BlockEntry(BlockID blockID, float weight = 1)
        {
            this.blockID = blockID;
            this.weight = weight;
        }
    }

    [SerializeField] 
    private List<BlockEntry> list;
}