// Block, the most basic unit, one block occupies one grid
using System;
using System.Collections;
using UnityEngine;

[Serializable]
public abstract class Block
{
    protected Block()
    {

    }

    // General
    public abstract BlockType Type { get; }
    public abstract string Name { get; }
    public virtual Sprite texture => BlockResources.blockTexture[Name];

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



    // General state change point for blocks
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

    /// <summary>
    /// Set position
    /// </summary>
    public void SetPosition(Vector2Int position)
    {
        this.position = position;
    }
    public Vector2Int MapPosition => position;
    public Vector3 GetWorldPosition() => MapBoundaryData.GridToWorld(position);

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



}

public enum BlockType
{
    Null,
}

// TODO: use namspace(class) storing name strings of blocks
