public class PistonHead : Block
{
    public override BlockID ID => BlockID.PistonHead;
    public override bool isInMap => pistonBase.isInMap;
    public override bool isLocked => pistonBase.isLocked;
    public override bool isEnabled => pistonBase.isEnabled;
    public override bool isAnimating => pistonBase.isAnimating;

    public override bool IsPushable => false;

    public void Init(Piston pistonBase)
    {
        this.pistonBase = pistonBase;
        pistonBase.OnRemoved += OnBaseRemoved;
        pistonBase.OnDestroyed += OnBaseDestroyed;
        transform.SetParent(pistonBase.transform);
    }

    private Piston pistonBase;

    private void OnBaseRemoved(Block b)
    {
        if (!isRemoved)
            map.RemoveBlock(this);
    }

    private void OnBaseDestroyed(Block b)
    {
        if (!isRemoved)
            map.DestroyBlock(this);
    }
}
