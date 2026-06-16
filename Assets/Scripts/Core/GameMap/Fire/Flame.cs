using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single fire instance. Handles aging, burn damage, and visual/audio.
/// Lifecycle (spawn/destroy) is owned by FireManager — Flame never destroys itself directly.
/// </summary>
public class Flame : MapObject, IRandomTickable
{
    public event Action<int> OnRandomTickUpdate;

    public Vector2Int position => BoundaryDataManager.GetBoundaryData(map.PlayerID).WorldToGrid(transform.position);
    public int age;
    public float damage = 1f;

    private int maxAge = 15;
    private FlammableObject attachedFlammable;
    private Vector2Int offset;
    private FireManager fireManager;

    private List<FlammableObject> adjacentFlammableBlocks = new List<FlammableObject>();

    [SerializeField] AudioClip extinguishSound;

    public void Init(MapManager map, FireManager fireManager, FlammableObject attachedFlammable, Vector2Int offset)
    {
        this.map = map;
        this.fireManager = fireManager;
        this.attachedFlammable = attachedFlammable;
        this.offset = offset;

        attachedFlammable.SetBurningAt(offset, this);
        transform.parent = attachedFlammable.transform;
        transform.localPosition = (Vector2)offset;

        age = 0;

        map.RandomTickManager.Register(this);
    }

    /// <summary>
    /// Unregister this flame from its FlammableObject and random tick system. Called by FireManager before destroying.
    /// </summary>
    public void DetachFromFlammable()
    {
        attachedFlammable.StopBurningAt(offset);
        map.RandomTickManager.Unregister(this);
    }

    /// <summary>
    /// Safety cleanup for when this flame's GameObject is destroyed externally
    /// (e.g. parent block destroyed), bypassing the normal Die/Extinguish path.
    /// </summary>
    private void OnDestroy()
    {
        map?.RandomTickManager?.Unregister(this);
        fireManager?.UnregisterFlame(this);
    }

    public void ResetFlame(int randomTick)
    {
        age = 0;
        // when re-set burn once
        Burn(randomTick);
    }

    public void RandomTickUpdate(int randomTick)
    {
        if (randomTick % 7 == 0)
            Burn(randomTick);
        if (randomTick % 1 == 0)
            AgeGrow(randomTick);

        OnRandomTickUpdate?.Invoke(randomTick);
    }

    /// <summary>
    /// Extinguish with sound effect (e.g. by water). Delegates destruction to FireManager.
    /// </summary>
    public void Extinguish()
    {
        AudioManager.Instance.PlaySFXAtPoint(extinguishSound, transform.position, 1f, AudioBus.Block);
        fireManager.DestroyFlame(this);
    }

    /// <summary>
    /// Silent death (aged out or manually removed). Delegates destruction to FireManager.
    /// </summary>
    public void Die()
    {
        fireManager.DestroyFlame(this);
    }

    public void Burn(int randomTick)
    {
        DetectAdjacent();

        int i = 0;
        foreach (FlammableObject targetBlock in adjacentFlammableBlocks)
        {
            if (randomTick % 2 == i || targetBlock == attachedFlammable)
                targetBlock.TakeBurnDamage(damage * Mathf.Lerp(1, 16, age / maxAge));

            if (targetBlock.IsDead())
            {
                targetBlock.BurnAway();
            }

            i++;
        }
    }

    private void AgeGrow(int randomTick)
    {
        age++;

        Burn(randomTick);

        if (age > maxAge)
        {
            Die();
        }
    }

    private void DetectAdjacent()
    {
        adjacentFlammableBlocks.Clear();
        foreach (var offset in new Vector2Int[] { Vector2Int.zero, Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up })
        {
            int x = position.x + offset.x;
            int y = position.y + offset.y;
            Block adjacentBlock = map.GetBlock(x, y);
            if (adjacentBlock != null)
            {
                if (adjacentBlock.GetComponent<FlammableObject>() is FlammableObject flammableBlock)
                {
                    adjacentFlammableBlocks.Add(flammableBlock);
                }
            }
        }
    }
}
