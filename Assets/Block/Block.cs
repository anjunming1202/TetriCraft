// Block, the most basic unit, one block occupies one grid
using System;
using UnityEngine;

[Serializable]
public abstract class Block
{
    public abstract BlockType Type { get; }
    public abstract string Name { get; }
    public virtual Sprite texture => BlockResources.blockTexture[Name];

    // Block position
    private Vector2Int _position = Vector2Int.zero; // grid position, use position
    public Vector2Int position
    {
        get => _position;
        set
        {
            _position = value;
            TriggerOnMoved();
        }
    }

    // Block state flags
    public bool isMoving = false;


    protected Block()
    {

    }

    /*public void MoveTo(Map grid, int x, int y)
    {
        MoveTo(grid, new Vector2Int(x, y));
    }
    public void MoveTo(Map grid, Vector2Int to)
    {
        grid[to.x, to.y] = this;
        grid[position.x, position.y] = null;
        position = to;
    }*/



    /////////////////////////////////////////////

    // Event for connecting the renderer
    public delegate void OnChangedEvent(Block data);
    public event OnChangedEvent OnMoved;


    // Trigger event when position changed
    public void TriggerOnMoved()
    {
        OnMoved?.Invoke(this);
    }
}

public enum BlockType
{
    Null,
}

// TODO: use namspace(class) storing name strings of blocks
