
using BlockSystem;
using UnityEngine;

public abstract class FluidDummy : Block
{
    public override bool IsDummy => true;

    public void Init(FluidManager fluidManager, FluidElement sourceElement)
    {
        FluidManager = fluidManager;
        SetSourceElement(sourceElement);
    }

    public void SetSourceElement(FluidElement element)
    {
        sourceElement = element;
    }

    public FluidElement GetSourceElement() => sourceElement;

    public override bool CanBeReplacedBy(Block block)
    {
        return true;
    }

    public override ReplacementDisposition GetReplacementDisposition(Block incoming)
    {
        return ReplacementDisposition.Remove;
    }

    public override bool IsClearable()
    {
        if (sourceElement.isFalling)
            return false;

        if (sourceElement.lowerLevel > FluidElement.Local2Level(GridPosition.y, 0) || sourceElement.upperLevel < FluidElement.Local2Level(GridPosition.y + 1, 0))
            return false;

        return true;
    }

    public override void OnPostMoved()
    {
        base.OnPostMoved();
        Debug.LogError($"Shouldn't move the fluid dummy block, {GridPosition}");
    }

    public override void OnPostDestroyed()
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

        base.OnPostDestroyed();
    }

    protected FluidManager FluidManager { get; private set; }
    protected FluidElement sourceElement;
}