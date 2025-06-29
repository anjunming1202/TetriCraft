using System;
using UnityEngine;

public class FluidElement : MonoBehaviour
{
    public BlockID ID;

    public int column;
    public float upperLevel;
    public float lowerLevel;
    public float midLevel => (upperLevel + lowerLevel) / 2;
    public Vector2 mapPosition => new Vector2(column, midLevel - 0.5f);
    public float height => upperLevel - lowerLevel;
    public float width => 1f;

    public bool isFalling;


    public float Local2Map(int y, float localLevel)
    {
        return y + localLevel;
    }

    public void FlowDownwards(float amount)
    {
        upperLevel -= amount;
        lowerLevel -= amount;
    }
}