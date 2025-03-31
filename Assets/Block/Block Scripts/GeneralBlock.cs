using UnityEngine;

public class GeneralBlock : Block
{
    [SerializeField] private BlockID id;
    public override BlockID ID => id;
}
