using System.Collections.Generic;
using UnityEngine;

public abstract class FluidDummy : Block
{
    public override bool IsDummy => true;

    public override bool CanBeReplacedBy(Block block)
    {
        return true;
    }

    public override void OnReplacedBy(Block block)
    {
        map.RemoveBlock(this);
    }

    public override void OnLockdown()
    {
        FluidManager.dummyBlockPositions.Add(GridPosition);
        base.OnLockdown();
    }

    public override void Remove()
    {
        FluidManager.dummyBlockPositions.Remove(GridPosition);
        base.Remove();
    }

    public override void Destroy()
    {
        int x = GridPosition.x;
        int y = GridPosition.y;

        //List<FluidElement> elements = FluidManager.fluidSystem.GetFluidElements(x, y);

        FluidElement parentElement = FluidManager.fluidSystem.GetFluidElements(x, y)[0];
        int upperGridLevel = FluidElement.Local2Level(y + 1, 0);
        int lowerGridLevel = FluidElement.Local2Level(y, 0);

        FluidManager.SplitElement(parentElement, upperGridLevel);
        parentElement = FluidManager.SplitElement(parentElement, lowerGridLevel);

        FluidManager.fluidSystem.Remove(parentElement);

        base.Destroy();
    }

    protected abstract FluidManager FluidManager { get; }
}