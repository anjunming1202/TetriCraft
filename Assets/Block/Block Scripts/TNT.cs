using UnityEngine;

public class TNT : Block
{
    public PrimedTNT primedTNTPrefab;

    public override BlockID ID => BlockID.TNT;

    public void Ignite()
    {
        if (!isLocked)
            return;
        PrimedTNT primedTNTInstance = Instantiate(primedTNTPrefab, transform.position, Quaternion.identity);
        primedTNTInstance.OnSpawned(map, CentrePosition);
        map.RemoveBlock(this);
    }

    protected override void Exploded()
    {
        Ignite();
    }
}
