using UnityEngine;

public class WaterBlock : FluidBlock
{
    [HideInInspector] public override BlockID ID => BlockID.Water;
    protected override FluidManager FluidManager => MapManager.WaterManager;
}