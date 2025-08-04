using UnityEngine;

public class WaterDummy : FluidDummy
{
    [HideInInspector] public override BlockID ID => BlockID.WaterDummy;
    protected override FluidManager FluidManager => MapManager.WaterManager;
}
