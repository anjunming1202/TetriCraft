// Block, the most basic unit, one block occupies one grid
using System;
using System.Collections;
using UnityEngine;

[Serializable]
public abstract class Block
{
    public Block()
    {
        this.Name = BlockResources.GetPrefab(Type).name;
    }
    public Block(string name)
    {
        this.Name = name;
    }

    // Identity
    public virtual string Name { get; }
    public abstract BlockType Type { get; }

    // Data in the map
    private Vector2Int position = Vector2Int.zero; // Block position in the map

    // Block state flags
    public bool isInMap = false;
    public bool isFalling = false;
    public bool isMoving = false;

    // Events
    public delegate void OnChangedEvent(Block block);
    public event OnChangedEvent OnPositionChanged;
    public event OnChangedEvent OnMoved;
    public event OnChangedEvent OnRotated;
    public event OnChangedEvent OnLanded;

    public delegate void OnInstantiatedEvent();
//  public event OnChangedEvent OnSpawned;
    public event OnInstantiatedEvent OnDestroy;



    // Position & Moving
    public Vector2Int MapPosition => position;
    public Vector3 GetWorldPosition() => MapBoundaryData.GridToWorld(position);
    /// <summary>
    /// Set position directly
    /// </summary>
    public void SetPosition(Vector2Int position)
    {
        this.position = position;
    }

    /// <summary>
    /// Move the block, trigger move event
    /// </summary>
    public void MoveTo(Vector2Int to)
    {
        // Set data of block self
        SetPosition(to);
        isMoving = true;
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
        isFalling = true;
    }

    public void Destroy()
    {
        OnDestroy?.Invoke();
    }

    public void Land()
    {
        isFalling = false;
        OnLanded?.Invoke(this);
    }

    public void StopMoving()
    {
        isMoving = false;
    }



}