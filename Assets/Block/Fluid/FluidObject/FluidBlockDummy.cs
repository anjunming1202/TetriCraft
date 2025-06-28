using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class FluidBlockDummy : Block
{
    public override bool OnTryReplacedBy(Block block)
    {
        return true;
    }
}
