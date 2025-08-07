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

        if (health <= 0)
        {
            OnExplosionDestroy?.Invoke();
        }
    }

    public bool IsDead()
    {
        return health <= 0;
    }

    public void ResetHealth()
    {
        health = blastResistance;
    }

    [SerializeField] private float health;
}
