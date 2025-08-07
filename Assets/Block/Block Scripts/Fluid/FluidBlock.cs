using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class FluidBlock : Block
{
    public override bool IsFluid => true;

    public override void OnLockdown()
    {
        base.OnLockdown();
        FluidManager.SpawnElement(GridPosition.x, FluidElement.Local2Level(GridPosition.y, 0));
        map.RemoveBlock(this);
    }

    protected abstract FluidManager FluidManager { get; }
}
