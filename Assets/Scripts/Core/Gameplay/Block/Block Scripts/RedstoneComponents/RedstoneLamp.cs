using UnityEngine;

public class RedstoneLamp : Block, IRedstoneActivatable
{
    public override BlockID ID => BlockID.RedstoneLamp;

    void IRedstoneActivatable.OnRedstoneActivated()
    {
        blockRenderer.ChangeState(1);
        OnTriggerAppearanceChanged();
    }

    void IRedstoneActivatable.OnRedstoneDeactivated()
    {
        blockRenderer.ChangeState(0);
        OnTriggerAppearanceChanged();
    }   

    protected override void Awake()
    {
        base.Awake();
        blockRenderer = GetComponent<BlockRenderer>();
    }

    private BlockRenderer blockRenderer;
}
