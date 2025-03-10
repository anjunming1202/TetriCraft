// Block, the most basic unit, one block occupies one grid
using System;
using System.Collections;
using UnityEngine;

[Serializable]
public abstract class Block
{
    public Block()
    {
        this.Name = BlockRegistry.GetMetadata(Type).Name;
    }

    // Identity
    public virtual string Name { get; }
    public abstract BlockID Type { get; }

    // Data in the map
    private Vector2Int position = Vector2Int.zero; // Block position in the map

    // Block state flags
    public bool isInMap = false;        // is in the map data
    public bool isLocked = false;       // is locked => ready to be cleared
    public bool isAnimating = false;    // is moving with animation

    // Events
    public delegate void OnChangedEvent(Block block);
    public event OnChangedEvent OnPositionChanged;
    public event OnChangedEvent OnMoved;
    public event OnChangedEvent OnPlaced;

    public delegate void OnInstantiatedEvent();
//  public event OnChangedEvent OnSpawned;
    public event OnInstantiatedEvent OnDestroyed;



    // Position & Moving
    public Vector2Int MapPosition => position;
    public Vector3 GetWorldPosition() => MapBoundaryData.GridToWorld(position);

    /// <summary>
    /// Set position directly
    /// </summary>
    public void SetPosition(Vector2Int position)
    {
        this.position = position;
        OnPositionChanged?.Invoke(this);
    }

    /// <summary>
    /// Move the block, trigger move event
    /// </summary>
    public void MoveTo(Vector2Int to)
    {
        // Set data of block self
        position = to;
        // Invoke block move event
        OnMoved?.Invoke(this);
    }
    /// <summary>
    /// Move the block, trigger move event
    /// </summary>
    public void MoveBy(int x, int y)
    {
        MoveTo(position + new Vector2Int(x, y));
    }



    // General Behaviour of Block
    public void Spawn()
    {
        
    }

    public void SpawnFalling()
    {
        Spawn();
        isLocked = false;
    }

    public void Destroy()
    {
        OnDestroyed?.Invoke();
    }

    public void Lockdown()
    {
        isLocked = true;
        OnPlaced?.Invoke(this);
    }



}