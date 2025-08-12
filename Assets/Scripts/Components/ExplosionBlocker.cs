using System;
using UnityEngine;

public class ExplosionBlocker : MonoBehaviour
{
    public float blastResistance;
    public bool isUnbreakable = false;

    public Action OnExplosionDestroy;

    private void Start()
    {
        health = blastResistance;
    }

    public void TakeDamage(float amount)
    {
        if (isUnbreakable)
            return;

        health -= amount;
    }

    public bool IsDead()
    {
        return health <= 0;
    }

    public void Die()
    {
        OnExplosionDestroy?.Invoke();
    }

    public void ResetHealth()
    {
        health = blastResistance;
    }

    [SerializeField] private float health;
}
