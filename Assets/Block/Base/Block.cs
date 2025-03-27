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
    public Vector2 Position => position;
    public Vector2Int GridPosition => new Vector2Int((int)position.x, (int)position.y);

    // Block state flags
    public bool isInMap = false;        // is in the map data
    public bool isLocked = false;       // is locked => ready to be cleared
    public bool isAnimating = false;    // is moving with animation

    // Events
    public delegate void OnChangedEvent(Block block);
    public event OnChangedEvent OnPositionChanged;
    public event OnChangedEvent OnDestroyed;

    public delegate void OnAnimationUpdateEvent();
    public event OnAnimationUpdateEvent OnInstantMove;
    public event OnAnimationUpdateEvent OnAnimatedMove;
    public event OnAnimationUpdateEvent OnLockedDown;



    // GridPosition & Moving
    public Vector3 GetWorldPosition()
    {
        return MapBoundaryData.MapToWorld(position);
    }

    public void SetPosition(int x, int y, bool animation = false)
    {
        position = new Vector2Int(x, y);
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




    public void Destroy()
    {
        GameObject.Destroy(gameObject);
        OnDestroyed?.Invoke(this);
    }

    public void Remove()
    {
        GameObject.Destroy(gameObject);
    }

    public void Lockdown()
    {
        isLocked = true;
        OnLockedDown?.Invoke();
    }

    public virtual void OnUpdate(Map map)
    {

    }



}