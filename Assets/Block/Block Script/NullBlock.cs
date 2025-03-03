using System;
using UnityEngine;

[Serializable]
public class NullBlock : Block
{
    public override BlockType Type => BlockType.Null;
    public override string Name => "missing_block";

    public NullBlock() : base() { }
}
