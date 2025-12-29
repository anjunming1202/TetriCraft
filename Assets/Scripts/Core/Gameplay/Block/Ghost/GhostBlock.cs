using System;
using UnityEngine;

public class GhostBlock : Block
{
    [HideInInspector] public override BlockID ID => BlockID.Missing;

    public Block shadowedBlock = null;

    public void Shadow(Block block)
    {
        shadowedBlock = block;
    }
}
