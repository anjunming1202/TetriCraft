// Block, the most basic unit, one block occupies one grid
using System;
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
    public bool isFalling = false;

    // Events
    public delegate void OnChangedEvent(Block block);
    public event OnChangedEvent OnChanged;
    public event OnChangedEvent OnLanded;



    // Getter & Setter
    public Vector2Int position // position
    {
        get => _position;
        set
        {
            _position = value;
            TriggerMoving();
        }
    }

    // On position changed
    public void TriggerMoving()
    {
        OnChanged?.Invoke(this);
    }

    // On landed
    public void TriggerLanding()
    {
        OnChanged?.Invoke(this);
    }

}

public enum BlockType
{
    Null,
}

// TODO: use namspace(class) storing name strings of blocks
