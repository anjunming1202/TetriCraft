using System;
using UnityEngine;

[Serializable]
public class NullBlock : Block
{
    public override BlockID ID => BlockID.Missing;
}
