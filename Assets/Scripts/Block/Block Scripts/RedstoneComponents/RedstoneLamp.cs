using UnityEngine;

public class RedstoneLamp : Block
{
    public override BlockID ID => BlockID.RedstoneLamp;

    public override void OnRedstoneActivated()
    {
        blockRenderer.ChangeState(1);
        OnTriggerAppearanceChanged();
    }

    public override void OnRedstoneDeactivated()
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
