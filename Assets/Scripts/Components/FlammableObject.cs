using System;
using System.Collections.Generic;
using UnityEngine;

public class FlammableObject : MonoBehaviour
{
    public static float burningDurability = 100f;
    public bool isFlammable = true;
    public int igniteAbility;
    public int burnAbility;
    public bool canBurnAway = true;

    public Action OnBurnAway;

    private void Start()
    {
        health = burningDurability;
    }

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
        return flamePositions[offset];
    }

    public void TakeBurnDamage(float amount = 1f)
    {
        if (!canBurnAway)
            return;

        health -= amount * burnAbility;
    }

    public bool IsDead()
    {
        return health < 0;
    }

    public void BurnAway()
    {
        OnBurnAway?.Invoke();
    }

    public void StopBurningAt(Vector2Int offset)
    {
        flamePositions.Remove(offset);

        if (flamePositions.Count == 0)
        {
            ResetHealth();
        }
    }

    private void ResetHealth()
    {
        health = burningDurability;
    }

    private float health;
    private Dictionary<Vector2Int, Flame> flamePositions = new Dictionary<Vector2Int, Flame>();
}
