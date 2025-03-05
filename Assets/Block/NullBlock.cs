using System;
using UnityEngine;

[Serializable]
public class NullBlock : Block
{
    public override BlockType Type => BlockType.Null;
    public NullBlock() : base(BlockResources.GetPrefab(BlockType.Null).name) { }

}
