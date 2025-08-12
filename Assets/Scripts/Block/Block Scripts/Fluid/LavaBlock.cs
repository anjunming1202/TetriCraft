using UnityEngine;

public class LavaBlock : FluidBlock
{
    [HideInInspector] public override BlockID ID => BlockID.Lava;
    protected override FluidManager FluidManager => MapManager.LavaManager;
}