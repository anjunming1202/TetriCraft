using UnityEngine;

public class TNT : Block, IRedstoneActivatable
{
    public PrimedTNT primedTNTPrefab;

    public override BlockID ID => BlockID.TNT;

    public void Ignite(float fuseTime = 4f)
    {        
        PrimedTNT primedTNTInstance = Instantiate(primedTNTPrefab, transform.position, Quaternion.identity);
        primedTNTInstance.Init(fuseTime);

        map.RequestSpawnEntity(primedTNTInstance, CentrePosition.x, CentrePosition.y);

        map.RequestRemoveBlock(this);
    }

    void IRedstoneActivatable.OnRedstoneActivated()
    {
        Ignite(4f);
    }

    void IRedstoneActivatable.OnRedstoneDeactivated()
    {

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
