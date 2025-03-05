using System;
using UnityEngine;

[Serializable]
public class NormalBlock : Block
{
    public override string Name { get; }
    public override BlockType Type { get; }
    public NormalBlock(BlockType type)
    { 
        Type = type;
        Name = BlockResources.GetPrefab(Type).name;
    }
    public NormalBlock(string name, BlockType type) : base(name) 
    {
        Type = type;
    }
}
