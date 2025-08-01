using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class FluidBlock : Block
{
    public override bool CanBeReplacedBy(Block block)
    {
        return true;
    }

    public override void OnLockdown(MapManager map)
    {
        base.OnLockdown(map);
    }
}
