using UnityEngine;

public class WaterDummy : FluidDummy
{
    public override BlockID ID => BlockID.WaterDummy;
    public override FluidManager FluidManager => MapManager.WaterManager;
}
