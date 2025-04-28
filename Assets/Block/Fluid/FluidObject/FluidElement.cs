using System;
using UnityEngine;

public class FluidElement : MonoBehaviour
{
    [SerializeField] private BlockID ID;

    public Vector2Int position;
    public float lowerLevel; // 0 ~ 1
    public float height;
    public float upperLevel => lowerLevel + height;

    public bool isFlowing;  // flowing downwards

    public FluidUpdatingState updatingState;


    public float absoluteLowerLevel => lowerLevel + position.y;
    public float absoluteUpperLevel => upperLevel + position.y;

    public void FlowsDownwards(float amount)
    {
        lowerLevel -= amount;
        if (lowerLevel < 0)
        {
            position.y--;
            lowerLevel++;
        }
        isFlowing = true;
    }

    public void FlowsInto(FluidElement elementTo, float amount)
    {
        // Debug.Assert(elementTo.lowerLevel == 0, $"Wrongly flowing horizontally at {position}");


        if (height - amount <= 0)
        {
            elementTo.height += height;
            height = 0;
        }
        else
        {
            elementTo.height += amount;
            height -= amount;
        }
    }

    public bool CheckCollide(float level)
    {
        return level >= lowerLevel && level <= upperLevel;
    }

    public bool CheckCollide(FluidElement element)
    {
        return element.upperLevel >= lowerLevel && element.lowerLevel <= upperLevel;
    }

    public void Delete()
    {
        GameObject.Destroy(gameObject);
    }
}

public enum FluidUpdatingState
{
    Waiting,
    Updating,
    Finished
}