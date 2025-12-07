using UnityEngine;

[CreateAssetMenu(menuName = "Block/BlockProperty")]
public class BlockProperty : ScriptableObject
{
    // Identity
    public BlockID ID;
    public string Name;

    // General Properties
    public bool IsDummy = false;
    public bool IsFluid = false;
    public bool IsOriented = false;
    public bool IsPushable;

    // Explosion Response Properties

    // Burning Response Properties
    public bool IsBurnable;

    // Redstone Response Properties

}
