using System;
using UnityEngine;

[Serializable]
public class NormalBlock : Block
{
    public override string Name { get; }
    public override BlockID Type { get; }
    public NormalBlock(BlockID type)
    { 
        Type = type;
        this.Name = BlockRegistry.GetMetadata(Type).Name;
    }
}
