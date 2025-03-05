using System;
using UnityEngine;

[Serializable]
public class NormalBlock : Block
{
    public override BlockType Type => BlockType.Null;

    public NormalBlock(string name) : base(name) { }
}
