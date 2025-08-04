using UnityEngine;

public class LavaDummy : FluidDummy
{
    [HideInInspector] public override BlockID ID => BlockID.LavaDummy;
    protected override FluidManager FluidManager => MapManager.LavaManager;
}
