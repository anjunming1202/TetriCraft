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
    private Vector2Int _position = Vector2Int.zero; // Block position in the map

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




    // Getter & Setter
    public Vector2Int position // position
    {
        get => _position;
        set
        {
            _position = value;
            isMoving = true;
            OnMoved?.Invoke(this);
        }
    }
}

public enum BlockType
{
    Null,
}

// TODO: use namspace(class) storing name strings of blocks
