using System;
using System.Collections.Generic;
using UnityEngine;

public class FlammableObject : MonoBehaviour
{
    public bool isFlammable = true;
    /// <summary>How likely nearby fire spreads TO this block (0–300). Mirrors Minecraft encouragement.</summary>
    public int encouragement;
    /// <summary>How quickly this block is destroyed by fire (0–100). 0 = immune to destruction. Mirrors Minecraft flammability.</summary>
    public int flammability;
    /// <summary>
    /// Whether lava can ignite this block or cause fire in adjacent air (0 = cannot, >0 = can).
    /// Distinct from encouragement — a block can be lava-igniteable without being fire-spreadable and vice versa.
    /// Mirrors Minecraft's separate lava ignitability property.
    /// </summary>
    public int lavaIgnitability;

    public Action OnBurnAway;

    public void SetBurningAt(Vector2Int offset, Flame flame)
    {
        Debug.Assert(flamePositions.TryAdd(offset, flame), "reset flame");
    }

    public bool IsBurningAt(Vector2Int offset)
    {
        return flamePositions.ContainsKey(offset);
    }

    public Flame GetFlame(Vector2Int offset)
    {
        if (flamePositions.ContainsKey(offset))
            return flamePositions[offset];
        return null;
    }

    public void BurnAway()
    {
        OnBurnAway?.Invoke();
        OnBurnAway = null;
    }

    public void StopBurningAt(Vector2Int offset)
    {
        flamePositions.Remove(offset);
    }

    private Dictionary<Vector2Int, Flame> flamePositions = new Dictionary<Vector2Int, Flame>();

    public bool isBurning => flamePositions.Count > 0;
}
