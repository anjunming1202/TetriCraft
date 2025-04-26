using UnityEngine;

public class FluidElement : MonoBehaviour
{
    [SerializeField] private BlockID ID;

    public float lowerLevel; // 0 ~ 1
    public float upperLevel; // 0 ~ 1

    public bool hasUpdated;

    public float height => upperLevel - lowerLevel;

    public void FlowsDownwards(float height)
    {
        lowerLevel -= height;
        upperLevel -= height;
    }

    public bool CheckCollide(float level)
    {
        return level >= lowerLevel;
    }

    public void Delete()
    {
        GameObject.Destroy(gameObject);
    }
}