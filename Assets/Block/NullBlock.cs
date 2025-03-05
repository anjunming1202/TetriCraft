using System;
using UnityEngine;

[Serializable]
public class NullBlock : Block
{
    public override BlockType Type => BlockType.Null;
    public NullBlock() : base() { }
    public NullBlock(string name) : base(name) { }
}
