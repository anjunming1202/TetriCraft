using UnityEngine;

public class TNT : Block, IRedstoneActivatable
{
    public PrimedTNT primedTNTPrefab;

    public override BlockID ID => BlockID.TNT;

    public void Ignite(float fuseTime = 4f)
    {        
        PrimedTNT primedTNTInstance = Instantiate(primedTNTPrefab, transform.position, Quaternion.identity);
        primedTNTInstance.fuseTime = fuseTime;
        primedTNTInstance.OnSpawned(map, CentrePosition);
        map.RemoveBlock(this);
    }

    bool IRedstoneActivatable.OnRedstoneActivated()
    {
        Ignite(4f);
        return true;
    }

    bool IRedstoneActivatable.OnRedstoneDeactivated()
    {
        return true;
    }

    bool IRedstoneActivatable.CanActivatedBy(Block source)
    {
        return true;
    }

    protected override void OnExploded()
    {
        Ignite(Random.Range(0.5f, 1f));
    }

    protected override void OnBurnAway()
    {
        Ignite(4f);
    }
}
