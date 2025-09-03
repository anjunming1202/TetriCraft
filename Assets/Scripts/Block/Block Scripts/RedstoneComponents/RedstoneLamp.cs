using UnityEngine;

public class RedstoneLamp : Block, IRedstoneActivatable
{
    public override BlockID ID => BlockID.RedstoneLamp;

    bool IRedstoneActivatable.OnRedstoneActivated()
    {
        blockRenderer.ChangeState(1);
        OnTriggerAppearanceChanged();
        return true;
    }

    bool IRedstoneActivatable.OnRedstoneDeactivated()
    {
        blockRenderer.ChangeState(0);
        OnTriggerAppearanceChanged();
        return true;
    }

    bool IRedstoneActivatable.CanActivatedBy(Block source)
    {
        return true;
    }    

    protected override void Awake()
    {
        base.Awake();
        blockRenderer = GetComponent<BlockRenderer>();
    }

    private BlockRenderer blockRenderer;
}
