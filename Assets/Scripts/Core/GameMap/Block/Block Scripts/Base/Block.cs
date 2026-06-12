// Block, the most basic unit, one block occupies one grid
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using static Unity.Collections.AllocatorManager;
using static Unity.VisualScripting.Member;

public abstract class Block : MapRandomTickBehaviourObject
{
    // Identity
    public abstract BlockID ID { get; }
    public PlayerID PlayerID { get; private set; }

    // Data in the map
    private Vector2 position = Vector2.zero;            // Block position in the map
    protected Vector2 lastPosition = Vector2.zero;

    public enum Orientation { Up, Left, Down, Right }
    public Orientation orientation = Orientation.Up;

    // Block readonly properties
    public virtual bool IsDummy => false;
    public virtual bool IsFluid => false;
    public virtual bool IsOriented => false;
    public virtual bool IsPushable => isLocked;

    // Block state flags
    public virtual bool isInMap { get; set; }       // is in the block grid data
    public virtual bool isLocked { get; set; }      // is locked => ready to be cleared
    public virtual bool isEnabled { get; set; }     // is the block able to do block behaviour logic 
    public virtual bool isAnimating { get; set; }   // is moving with animation
    public virtual bool isRemoved { get; set; }

    public bool isActivated = false;
    public bool wasActivated = false;
    public bool isCharged = false;

    // Events
    public delegate void OnChangedEvent(Block block);
    public event OnChangedEvent OnMoved;
    public event OnChangedEvent OnAppearanceChanged;

    public delegate void OnAnimationUpdateEvent();
    public event OnAnimationUpdateEvent OnInstantMove;
    public event OnAnimationUpdateEvent OnAnimatedMove;

    public event OnChangedEvent OnSpawned;
    public event OnChangedEvent OnLockedDown;
    public event OnChangedEvent OnPendingDestroyed;
    public event OnChangedEvent OnPendingRemoved;
    public event OnChangedEvent OnAfterDestroyed;
    public event OnChangedEvent OnAfterRemoved;

    /*public event Action<Vector2Int> OnNCUpdateSent;
    public event Action<Block, Vector2Int> OnNCUpdateRequestReceived;
    public event Action<Vector2Int> OnPPBlockUpdated;*/



    public Vector2 Position => position;
    public Vector2 CentrePosition => position + Vector2.one * 0.5f;
    public Vector2Int GridPosition => GetGridPosition(position);

    public Vector3 GetWorldPosition()
    {
        return BoundaryDataManager.GetBoundaryData(PlayerID).MapToWorld(CentrePosition);
    }

    public Vector2Int Orientation2Direction(Orientation orientation)
    {
        return orientation switch
        {
            Orientation.Up => Vector2Int.up,
            Orientation.Down => Vector2Int.down,
            Orientation.Left => Vector2Int.left,
            Orientation.Right => Vector2Int.right,
            _ => Vector2Int.zero,
        };
    }
    public Orientation Direction2Orientation(Vector2Int dir)
    {
        return (dir.x, dir.y) switch
        {
            (0, 1) => Orientation.Up,
            (0, -1) => Orientation.Down,
            (1, 0) => Orientation.Right,
            (-1, 0) => Orientation.Left,
            _ => Orientation.Up
        };
    }

    public Vector2Int Facing => Orientation2Direction(orientation);

    public float Rotation => (int)orientation * 90;

    public virtual void SetGridPosition(int x, int y, bool animation = false)
    {
        SetPosition((float)x, (float)y, animation);
    }
    public void SetGridPosition(Vector2Int pos, bool animation = false) => SetGridPosition(pos.x, pos.y, animation);

    public void Rotate(bool clockwise)
    {
        if (!IsOriented)
            return;
        orientation = (Orientation)((((int)orientation + (clockwise ? -1 : 1)) + 4) % 4);
        OnTriggerAppearanceChanged();
    }

    public void OnTriggerAppearanceChanged()
    {
        OnAppearanceChanged?.Invoke(this);
    }

    /// <summary>
    /// Init the block
    /// </summary>
    public virtual void OnRegistered(MapManager map)
    {
        this.map = map;

        this.PlayerID = map.PlayerID;

        //SetPosition(x, y);

        isInMap = true;
        isLocked = false;
        isEnabled = true;
        isAnimating = false;
        isRemoved = false;

        isCharged = false;
    }

    public virtual void OnUnregistered()
    {
        isInMap = false;
        isLocked = false;
        isEnabled = false;
        isAnimating = false;
        isRemoved = true;

        isCharged = false;
    }

    public virtual void OnPostSpawned()
    {
        OnSpawned?.Invoke(this);
    }

    public virtual void OnPostMoved()
    {

    }

    public virtual void OnRequestingDestroy()
    {
        OnPendingDestroyed?.Invoke(this);
    }

    public virtual void OnRequestingRemove()
    {
        OnPendingRemoved?.Invoke(this);
    }

    /// <summary>
    /// Removed with breaking, don't use directly
    /// </summary>
    public virtual void OnPostDestroyed()
    {
        OnAfterDestroyed?.Invoke(this);
        OnPostRemoved();
    }

    /// <summary>
    /// Romove, don't use directly
    /// </summary>
    public virtual void OnPostRemoved()
    {
        OnAfterRemoved?.Invoke(this);
    }

    public virtual void OnLockdown()
    {
        Lockdown();
    }

    public virtual void OnUpdate()
    {
        if (this == null) return;
    }

    public virtual void OnNCUpdateRespond(Vector2Int updateSrc)
    {
        //Debug.Log($"neighbour updated {GridPosition}");

        // detect if the updated neighbour is a activation/charging source
        UpdateActivationState();
        UpdateChargingState();
    }

    public virtual bool CanBeReplacedBy(Block block)
    {
        return false;
    }

    public virtual void OnReplacedBy(Block block)
    {
        Debug.LogError($"{this} at {position} shouldn't be replaced by {block}");
    }

    public virtual bool IsClearable()
    {
        return isLocked;
    }

    public virtual void OnCharged(Vector2Int sourcePosition)
    {
        isCharged = true;

        //BlockUpdateManager.OnNeighbourChangedBlockUpdate(map.grid, GridPosition);
    }

    public virtual void OnDischarged(Vector2Int sourcePosition)
    {
        isCharged = false;

        //BlockUpdateManager.OnNeighbourChangedBlockUpdate(map.grid, GridPosition);
    }

    protected virtual void Awake()
    {
        // set explosion responses
        explosionTarget = GetComponent<ExplosionBlocker>();
        if (explosionTarget != null)
        {
            explosionTarget.OnExplosionDestroy += OnExploded;
            explosionTarget.isUnbreakable = true; // unlocked block is unbreakable
        }
        // set flame responses
        flammableObject = GetComponent<FlammableObject>();
        if (flammableObject != null)
        {
            flammableObject.OnBurnAway += OnBurnAway;
            flammableObject.isFlammable = false; // unlocked block is unflammable
        }
    }

    protected void SetPosition(float x, float y, bool animation = false)
    {
        lastPosition = position;
        position = new Vector2(x, y);
        OnMoved?.Invoke(this);

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

    protected void Lockdown()
    {
        // lock down state
        isLocked = true;
        OnLockedDown?.Invoke(this);

        //
        if (explosionTarget != null)
            explosionTarget.isUnbreakable = false;
        if (flammableObject != null)
            flammableObject.isFlammable = true;
    }

    protected virtual void OnExploded()
    {
        map.RequestDestroyBlock(this);
    }

    protected virtual void OnBurnAway()
    {
        map.RequestRemoveBlock(this);
    }

    private ExplosionBlocker explosionTarget;
    private FlammableObject flammableObject;

    // Redstone
    /// <summary>
    /// 
    /// </summary>
    private void UpdateActivationState()
    {
        if (this is not IRedstoneActivatable component)
            return;

        bool activated = false;
        foreach (var block in map.GetAdjacentBlocks(GridPosition.x, GridPosition.y, true))
        {
            if (block != null && component.CanActivatedBy(block))
            {
                activated = true;
                break;
            }
        }

        isActivated = activated;

        //map.RedstoneManager.AddUpdatedBlock(this);
    }

    private void UpdateChargingState()
    {

    }
}