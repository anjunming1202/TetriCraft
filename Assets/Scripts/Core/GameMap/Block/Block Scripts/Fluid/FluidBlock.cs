using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class FluidBlock : Block
{
    public abstract FluidID FluidID { get; }
    public override bool IsFluid => true;

    public override void OnLockdown()
    {
        base.OnLockdown();
        map.ImmediateBlockSqueezeFluids(this);
        FluidManager.SpawnElement(GridPosition.x, FluidElement.Local2Level(GridPosition.y, 0));
        map.RequestRemoveBlock(this);
    }

    protected FluidManager FluidManager => map.FluidSystem[FluidID];
}
