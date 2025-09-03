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

    // Data in the map
    private Vector2 position = Vector2.zero;            // Block position in the map
    protected Vector2 lastPosition = Vector2.zero;

    public enum Orientation { Up, Left, Down, Right }
    public Orientation orientation = Orientation.Up;

    // Block readonly properties
    public virtual bool IsDummy => false;
    public virtual bool IsFluid => false;
    public virtual bool IsOriented => false;
    public virtual bool IsPushable => true;

    // Block state flags
    public bool isInMap = false;        // is in the map data
    public bool isLocked = false;       // is locked => ready to be cleared
    public bool isAnimating = false;    // is moving with animation
    public bool isRemoved = false;

    public bool onActivation => activateSources.Count > 0;
    public bool isActivated = false;    // is activated in last frame
    public bool isCharged = false;

    // Events
    public delegate void OnChangedEvent(Block block);
    public event OnChangedEvent OnMoved;
    public event OnChangedEvent OnStateChanged;

    public delegate void OnAnimationUpdateEvent();
    public event OnAnimationUpdateEvent OnInstantMove;
    public event OnAnimationUpdateEvent OnAnimatedMove;

    public event Action OnLockedDown;
    public event Action OnDestroyed;
    public event OnChangedEvent OnRemoved;



    public Vector2 Position => position;
    public Vector2 CentrePosition => position + Vector2.one * 0.5f;
    public Vector2Int GridPosition => GetGridPosition(position);
    public Vector2Int LastGridPosition => GetGridPosition(lastPosition);

    public Vector3 GetWorldPosition()
    {
        return MapBoundaryData.MapToWorld(CentrePosition);
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

    public void SetPosition(int x, int y, bool animation = false)
    {
        SetPosition((float)x, (float)y, animation);
    }

    public void Rotate(bool clockwise)
    {
        if (!IsOriented)
            return;
        orientation = (Orientation)((((int)orientation + (clockwise ? -1 : 1)) + 4) % 4);
        OnTriggerAppearanceChanged();
    }

    public virtual void OnSpawn(MapManager map)
    {
        this.map = map;
        this.transform.SetParent(map.transform);

        isCharged = false;
    }

    public virtual void OnLockdown()
    {
        Lockdown();
        map.OnGridPlace?.Invoke(map, this);
    }

    public virtual void OnUpdate()
    {
        if (this == null) return;
    }

    public virtual void OnNeighbourUpdated(Vector2Int neighbourPos)
    {
        //Debug.Log($"neighbour updated {GridPosition}");
        
        // detect if the updated neighbour is a activation/charging source
        UpdateActivationSourceState(neighbourPos);
        UpdateChargingSourceState(neighbourPos);

        // detect if the updated neighbour can be activated/charged by this block
        UpdateActivationTargetState(neighbourPos);
        UpdateChargingTargetState(neighbourPos);
    }

    public virtual bool CanBeReplacedBy(Block block)
    {
        return false;
    }

    public virtual void OnReplacedBy(Block block)
    {
        Debug.LogError($"block {this} at {position} wrongly replaced");
    }

    public virtual bool IsClearable()
    {
        return isLocked;
    }

    /// <summary>
    /// Removed with breaking, don't use directly
    /// </summary>
    public virtual void Destroy()
    {
        isRemoved = true;
        OnDestroyed?.Invoke();
        Remove();
    }

    /// <summary>
    /// Romove, don't use directly
    /// </summary>
    public virtual void Remove()
    {
        isRemoved = true;
        OnRemoved?.Invoke(this);
        GameObject.Destroy(gameObject);
    }

    public virtual void OnCharged(Vector2Int sourcePosition)
    {
        isCharged = true;

        map.grid.OnBlockUpdate(GridPosition);
    }

    public virtual void OnDischarged(Vector2Int sourcePosition)
    {
        isCharged = false;

        map.grid.OnBlockUpdate(GridPosition);
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
        isLocked = true;
        OnLockedDown?.Invoke();

        if (explosionTarget != null)
            explosionTarget.isUnbreakable = false;
        if (flammableObject != null)
            flammableObject.isFlammable = true;
    }

    protected void OnTriggerAppearanceChanged()
    {
        OnStateChanged?.Invoke(this);
    }

    protected virtual void OnExploded()
    {
        map.DestroyBlock(this);
    }

    protected virtual void OnBurnAway()
    {
        map.RemoveBlock(this);
    }

    private ExplosionBlocker explosionTarget;
    private FlammableObject flammableObject;

    [SerializeField] protected Dictionary<Vector2Int, Block> activateSources = new Dictionary<Vector2Int, Block>();
    [SerializeField] protected Dictionary<Vector2Int, Block> chargeSources = new Dictionary<Vector2Int, Block>();
    [SerializeField] protected Dictionary<Vector2Int, Block> activateTargets = new Dictionary<Vector2Int, Block>();
    [SerializeField] protected Dictionary<Vector2Int, Block> chargeTargets = new Dictionary<Vector2Int, Block>();

    /// <summary>
    /// 
    /// </summary>
    private void UpdateActivationSourceState(Vector2Int sourcePos)
    {
        if (this is not IRedstoneActivatable component)
            return;

        Block source = map.GetBlock(sourcePos.x, sourcePos.y);

        // if the detected position is able to activate this block
        if (source != null && !source.isRemoved && source.isCharged && component.CanActivatedBy(source))
        {
            // detect if it's a new position
            if (!activateSources.ContainsKey(sourcePos))
            {
                activateSources.Add(sourcePos, source);
                source.activateTargets.Add(GridPosition, this);
            }
            // add this block to the redstone update list
            map.RedstoneManager.AddUpdatedBlock(this);
        }
        // if the detected position isn't able to activate this block
        else
        {
            // detect if it's an old position
            if (activateSources.ContainsKey(sourcePos))
            {
                activateSources.Remove(sourcePos);
                if (source != null)
                    source.activateTargets.Remove(GridPosition);
            }
            // add this block to the redstone update list (only when no sources present need an update check)
            if (activateSources.Count == 0)
                map.RedstoneManager.AddUpdatedBlock(this);
        }
    }

    private void UpdateActivationTargetState(Vector2Int targetPos)
    {
        Block target = map.GetBlock(targetPos.x, targetPos.y);

        if (target != null)
        {
            target.UpdateActivationSourceState(GridPosition);
        }
        else if (activateTargets.ContainsKey(targetPos))
        {
            target = activateTargets[targetPos];
            if (target != null)
                target.activateSources.Remove(GridPosition);
            activateTargets.Remove(targetPos);

            map.RedstoneManager.AddUpdatedBlock(target);//
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private void UpdateChargingSourceState(Vector2Int sourcePos)
    {

    }

    private void UpdateChargingTargetState(Vector2Int targetPos)
    {

    }
}