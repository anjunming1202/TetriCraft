// Block, the most basic unit, one block occupies one grid
using System;
using UnityEngine;

[Serializable]
public abstract class Block : MonoBehaviour
{
    private void Awake()
    {

    }

    // Identity
    public abstract BlockID ID { get; }

    // Data in the map
    private Vector2 position = Vector2.zero;            // Block position in the map

    // Block state flags
    public bool isInMap = false;        // is in the map data
    public bool isLocked = false;       // is locked => ready to be cleared
    public bool isClearable = false;    // can be cleared
    public bool isAnimating = false;    // is moving with animation

    // Events
    public delegate void OnChangedEvent(Block block);
    public event OnChangedEvent OnPositionChanged;
    public event OnChangedEvent OnDestroyed;

    public delegate void OnAnimationUpdateEvent();
    public event OnAnimationUpdateEvent OnInstantMove;
    public event OnAnimationUpdateEvent OnAnimatedMove;
    public event OnAnimationUpdateEvent OnLockedDown;



    public Vector2 Position => position;
    public Vector2Int GridPosition => GetGridPosition(position);
    public Vector3 GetWorldPosition()
    {
        return MapBoundaryData.MapToWorld(position);
    }

    public void SetPosition(int x, int y, bool animation = false)
    {
        SetPosition((float)x, (float)y, animation);
    }

    public virtual void OnUpdate(Map map)
    {

    }

    public virtual void OnLockdown()
    {
        isLocked = true;
        isClearable = true;
        OnLockedDown?.Invoke();
    }

    public void Destroy()
    {
        OnDestroyed?.Invoke(this);
        GameObject.Destroy(gameObject);
    }

    public void Remove()
    {
        GameObject.Destroy(gameObject);
    }



    protected void SetPosition(float x, float y, bool animation = false)
    {
        position = new Vector2(x, y);
        OnPositionChanged?.Invoke(this);

        if (animation)
        {
            OnAnimatedMove?.Invoke();
        }
        else
        {
            transform.position = GetWorldPosition();
            OnInstantMove?.Invoke();
        }
    }

    protected Vector2Int GetGridPosition(Vector2 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
    }
}

public enum BlockMapState
{
    NotInMap = -1,
    InTetromino,
    Grounding,
    Locked,
    NotClearable
}