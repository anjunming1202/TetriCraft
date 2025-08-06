using System;
using UnityEngine;

public class ExplosionTarget : MonoBehaviour
{
    public float blastResistance;

    public Action OnExplosionDestroy;

    private void Start()
    {
        health = blastResistance;
    }

    public void TakeDamage(float amount)
    {
        health -= amount;

        if (health <= 0)
        {
            OnExplosionDestroy?.Invoke();
        }
    }

    public bool IsDead()
    {
        return health <= 0;
    }

    private float health;
}
