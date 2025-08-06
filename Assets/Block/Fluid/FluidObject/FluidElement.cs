using System;
using UnityEngine;

public class FluidElement : MonoBehaviour
{
    public const int BlockAmount = 100;

    public BlockID ID;

    public int column;
    public int amount;
    public int lowerLevel;
    public int upperLevel => lowerLevel + amount;
    public int midLevel => (upperLevel + lowerLevel) / 2;
    public Vector2 mapPosition => new Vector2(column + 0.5f, (float)midLevel / BlockAmount);
    public float width => 1f;
    public float height => (float)amount / BlockAmount;
    public int lowerGridPosition => (lowerLevel >= 0 || lowerLevel % BlockAmount == 0) ? lowerLevel / BlockAmount : lowerLevel / BlockAmount - 1;
    public int upperGridPosition => (upperLevel <= 0 || upperLevel % BlockAmount == 0) ? upperLevel / BlockAmount - 1 : upperLevel / BlockAmount;
    public int localLowerLevel => (lowerLevel % BlockAmount + BlockAmount) % BlockAmount;
    public int localUpperLevel => (upperLevel % BlockAmount + BlockAmount) % BlockAmount;


    public bool isFalling;

    public FluidUpdatingState updatingState;
    public bool hasFlown;

    public int entrainmentDirection = -1;

    public static int Local2Level(int y, int localLevel)
    {
        return y * BlockAmount + localLevel;
    }

    public void FlowDownwards(int amount)
    {
        lowerLevel -= amount;
        isFalling = true;
    }

    public void FlowTo(FluidElement to, int amount)
    {
        this.amount -= amount;
        to.amount += amount;
    }

    public void ResetStates()
    {
        updatingState = FluidUpdatingState.Unupdated;
        isFalling = false;
        hasFlown = false;
        entrainmentDirection = -1;
    }
}

public enum FluidUpdatingState
{
    Unupdated,
    Waiting,
    Updated
}