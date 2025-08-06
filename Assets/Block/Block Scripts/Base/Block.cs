// Block, the most basic unit, one block occupies one grid
using System;
using UnityEngine;

public abstract class Block : MonoBehaviour
{
    // Identity
    public abstract BlockID ID { get; }

    // Data in the map
    private Vector2 position = Vector2.zero;            // Block position in the map

    // Block properties
    public virtual bool IsDummy => false;
    public virtual bool IsFluid => false;

    // Block state flags
    public bool isInMap = false;        // is in the map data
    public bool isLocked = false;       // is locked => ready to be cleared
    public bool isClearable = false;    // can be cleared
    public bool isAnimating = false;    // is moving with animation

    // Events
    public delegate void OnChangedEvent(Block block);
    public event OnChangedEvent OnPositionChanged;

    public delegate void OnAnimationUpdateEvent();
    public event OnAnimationUpdateEvent OnInstantMove;
    public event OnAnimationUpdateEvent OnAnimatedMove;

    public event Action OnLockedDown;
    public event Action OnDestroyed;



    public Vector2 Position => position;
    public Vector2Int GridPosition => GetGridPosition(position);

    public Vector3 GetWorldPosition()
    {
        return MapBoundaryData.MapToWorld(position + Vector2.one * 0.5f);
    }

    public void SetPosition(int x, int y, bool animation = false)
    {
        SetPosition((float)x, (float)y, animation);
    }

    private void Awake()
    {
        ExplosionTarget explosionTarget = GetComponent<ExplosionTarget>();
        if (explosionTarget != null)
            explosionTarget.OnExplosionDestroy += Remove;
    }

    public virtual void OnLockdown(MapManager map)
    {
        OnLockdown();
        map.OnGridPlace?.Invoke(map, this);
    }

    public virtual void OnUpdate(MapManager map)
    {
        if (this == null) return;
    }

    public virtual bool CanBeReplacedBy(Block block)
    {
        return false;
    }

    public virtual void OnReplacedBy(MapManager map, Block block)
    {
        Debug.LogError($"block {this} at {position} wrongly replaced");
    }

    /// <summary>
    /// Removed with breaking
    /// </summary>
    public virtual void Destroy()
    {
        OnDestroyed?.Invoke();
        Remove();
    }

    public virtual void Remove()
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

    protected void OnLockdown()
    {
        isLocked = true;
        isClearable = true;
        OnLockedDown?.Invoke();
    }
}