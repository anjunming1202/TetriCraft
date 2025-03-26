using System;
using System.Collections.Generic;
using UnityEngine;

public class RandomBlockPool : MonoBehaviour
{
    [Serializable]
    public struct BlockEntry
    {
        public BlockID blockID;
        public float weight;
        public BlockEntry(BlockID blockID, float weight = 1)
        { 
            this.blockID = blockID;
            this.weight = weight;
        }
    }

    public List<BlockEntry> pool;
}
