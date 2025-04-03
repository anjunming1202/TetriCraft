using UnityEngine;

public class FluidElement : MonoBehaviour
{
    [SerializeField] private BlockID ID;

    public float lowerLevel; // 0 ~ 1
    public float upperLevel; // 0 ~ 1
}