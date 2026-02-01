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

    public event Action OnLockedDown;
    public event Action OnDestroyed;
    public event OnChangedEvent OnRemoved;

    public event Action<Vector2Int> OnNCBlockUpdated;
    public event Action<Vector2Int> OnPPBlockUpdated;



    public Vector2 Position => position;
    public Vector2 CentrePosition => position + Vector2.one * 0.5f;
    public Vector2Int GridPosition => GetGridPosition(position);
    public Vector2Int LastGridPosition => GetGridPosition(lastPosition);

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

    public virtual void SetPosition(int x, int y, bool animation = false)
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

    public void OnTriggerAppearanceChanged()
    {
        OnAppearanceChanged?.Invoke(this);
    }

    public Coroutine Disable(float sec)
    {
        return StartCoroutine(DisableOnSet(sec));
    }

    public virtual void OnSpawn(MapManager map)
    {
        this.map = map;
        transform.SetParent(map.blockGrid.transform);

        this.PlayerID = map.PlayerID;

        isInMap = true;
        isLocked = false;
        isEnabled = true;
        isAnimating = false;
        isRemoved = false;

        isCharged = false;
    }

    public virtual void OnLockdown()
    {
        Lockdown();
        map.OnGridPlace?.Invoke(map, this);
        BlockUpdateManager.OnNeighbourChangedBlockUpdate(map.grid, GridPosition); // lockdown triggers a block update
    }

    public virtual void OnUpdate()
    {
        if (this == null) return;
    }

    public virtual void OnNCUpdateTriggered()
    {
        OnNCBlockUpdated?.Invoke(GridPosition);
    }

    public void NeighbourChangedNotified(Vector2Int updateSrc)
    {
        map.BlockUpdateManager.AddUpdatedBlock(this, updateSrc);
    }

    public virtual void NCNotificationUpdate(Vector2Int updateSrc)
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

        BlockUpdateManager.OnNeighbourChangedBlockUpdate(map.grid, GridPosition);
    }

    public virtual void OnDischarged(Vector2Int sourcePosition)
    {
        isCharged = false;

        BlockUpdateManager.OnNeighbourChangedBlockUpdate(map.grid, GridPosition);
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

        map.RedstoneManager.AddUpdatedBlock(this);
    }

    private void UpdateChargingState()
    {

    }

    private IEnumerator DisableOnSet(float sec)
    {
        isEnabled = false;
        yield return new WaitForSeconds(sec);
        isEnabled = true;
    }
}