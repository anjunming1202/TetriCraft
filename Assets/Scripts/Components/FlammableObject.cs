using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class FlammableObject : MonoBehaviour
{
    public static int ignitePoint = 100;
    public static int burningDurability = 100;
    public bool isFlammable = true;
    public int flammability;
    public int burnAbility;
    public bool canBurnAway = true;

    public Action OnBurnAway;

    private void Start()
    {
        ResetHealth();
    }

    public bool TryIgnite(float distance, float sourceStrength, List<FlammableObject> adjacents)
    {
        foreach (FlammableObject adjacent in adjacents)
        {
            // flame proof (water) stops ignitition
            if (adjacent.flammability < 0)
            {
                igniteProgress = 0;
                return false;
            }

            // ignite progress proceed
            igniteProgress += adjacent.flammability * Mathf.Lerp(2, 1, (distance) / 3) * sourceStrength;
        }

        if (igniteProgress >= ignitePoint)
            return true;
        return false;
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
        if (flamePositions.ContainsKey(offset))
            return flamePositions[offset];
        return null;
    }

    public void TakeBurnDamage(float amount)
    {
        if (!canBurnAway)
            return;

        burningHealth -= amount * burnAbility;
    }

    public bool IsDead()
    {
        return burningHealth <= 0;
    }

    public void BurnAway()
    {
        OnBurnAway?.Invoke();
        OnBurnAway = null;
    }

    public void StopBurningAt(Vector2Int offset)
    {
        flamePositions.Remove(offset);

        if (!isBurning)
        {
            ResetHealth();
        }
    }

    private void ResetHealth()
    {
        burningHealth = burningDurability;
        igniteProgress = 0;
    }

    [SerializeField, ReadOnly] private float igniteProgress;
    [SerializeField, ReadOnly] private float burningHealth;
    private Dictionary<Vector2Int, Flame> flamePositions = new Dictionary<Vector2Int, Flame>();

    private bool isBurning => flamePositions.Count > 0;
}
