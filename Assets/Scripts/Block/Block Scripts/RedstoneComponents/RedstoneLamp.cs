using UnityEngine;

public class RedstoneLamp : Block, IRedstoneActivatable
{
    public override BlockID ID => BlockID.RedstoneLamp;

    public void OnRedstoneActivated()
    {
        blockRenderer.ChangeState(1);
        OnTriggerAppearanceChanged();
    }

    public void OnRedstoneDeactivated()
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
