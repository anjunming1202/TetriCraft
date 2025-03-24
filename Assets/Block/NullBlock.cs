using System;
using UnityEngine;

[Serializable]
public class NullBlock : Block
{
    public override BlockID Type => BlockID.Null;
    public NullBlock() : base() { }
}
