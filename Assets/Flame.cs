using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flame : MapObject
{
    public Vector2Int position => MapBoundaryData.WorldToGrid(transform.position);
    public int age;
    public float damage = 1f;

    public void Init(MapManager map, FlammableObject attachedTarget, Vector2Int offset)
    {
        this.map = map;
        this.attachedTarget = attachedTarget;
        this.offset = offset;
        attachedTarget.SetBurningAt(offset, this);
        transform.parent = attachedTarget.transform;
        transform.localPosition = (Vector2)offset;
        age = 0;
    }

    public void Reset()
    {
        age = 0;
    }

    public void Extinguish()
    {
        attachedTarget.StopBurningAt(offset);
    }

    private void Update()
    {
        Burn();
    }

    private void Burn()
    {
        age++;
        attachedTarget.TakeBurnDamage(damage);

        // target burns away
        if (attachedTarget.IsDead())
        {
            attachedTarget.BurnAway();
        }

        // flame dies
        if (age > maxAge)
        {
            Extinguish();
        }
    }

    private int maxAge = 15;
    private FlammableObject attachedTarget;
    private Vector2Int offset;
}
