public class PistonHead : Block
{
    public override BlockID ID => BlockID.PistonHead;
    public override bool IsPushable => false;

    public void Init(Piston pistonBase)
    {
        this.pistonBase = pistonBase;
        pistonBase.OnRemoved += OnBaseRemoved;
        pistonBase.OnDestroyed += OnBaseDestroyed;
        transform.SetParent(pistonBase.transform);
    }

    private Piston pistonBase;

    private void OnBaseRemoved(Block block)
    {
        if (!isRemoved)
            map.RemoveBlock(this);
    }

    private void OnBaseDestroyed()
    {
        if (!isRemoved)
            map.DestroyBlock(this);
    }
}
