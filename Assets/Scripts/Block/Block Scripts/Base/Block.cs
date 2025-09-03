// Block, the most basic unit, one block occupies one grid
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

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

        foreach (Block block in map.GetAdjacentBlocks(GridPosition.x, GridPosition.y))
        {
            if (block != null)
            {
                UpdateActivationState(block); // for oncharge no need to foreach
            }
        }
    }

    public virtual void OnDischarged(Vector2Int sourcePosition)
    {
        isCharged = false;

        /*foreach (Block block in map.GetAdjacentBlocks(GridPosition.x, GridPosition.y))
        {
            if (block is RedstonePassiveComponent component)
            {
                component.RedstoneSourceDeactivated(GridPosition);
            }
        }*/
    }

    public virtual void OnUpdateRedstoneStates()
    {
        UpdateActivationState(this);
        UpdateChargingState(this);
    }

    public virtual void OnNeighbourUpdated()
    {
        //throw new NotImplementedException();
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

    [SerializeField] protected List<Vector2Int> activateSources = new List<Vector2Int>();
    [SerializeField] protected List<Vector2Int> activateTargets= new List<Vector2Int>();
    [SerializeField] protected List<Vector2Int> chargeSources = new List<Vector2Int>();
    [SerializeField] protected List<Vector2Int> chargeTargets = new List<Vector2Int>();

    /// <summary>
    /// Update when added/removed.
    /// </summary>
    private static void UpdateActivationState(Block block)
    {
        if (block is not IRedstoneActivatable component)
            return;

        // detect new sources
        int pointer = 0;
        foreach (Block adjacent in block.map.GetAdjacentBlocks(block.GridPosition.x, block.GridPosition.y, true))
        {
            if (adjacent != null && adjacent.isInMap && !adjacent.isRemoved && adjacent.isCharged && component.CanActivatedBy(adjacent)) // a valid source found
            {
                if (block.activateSources.Contains(adjacent.GridPosition)) // valid old sources
                    block.activateSources.Remove(adjacent.GridPosition);   
                else                                                       // valid new sources
                    adjacent.activateTargets.Add(block.GridPosition);

                if (pointer > block.activateSources.Count)
                {

                }

                block.activateSources.Insert(pointer, adjacent.GridPosition);
                pointer++;
            }
        }

        // remove invalid old sources
        for (int i = block.activateSources.Count - 1; i >= pointer; i--)
        {
            Vector2Int invalidSourcePosition = block.activateSources[i];
            block.activateSources.RemoveAt(i);
            Block source = block.map.GetBlock(invalidSourcePosition.x, invalidSourcePosition.y);
            if (source != null)
                source.activateTargets.Remove(block.GridPosition);
        }

        // add to redstone manager update list
        block.map.RedstoneManager.AddUpdatedBlock(block);
    }

    /// <summary>
    /// Update when added/removed.
    /// </summary>
    private static void UpdateChargingState(Block block)
    {
        // change charging state



        // update activation

        // remove old targets
        Vector2Int[] activateTargets = block.activateTargets.ToArray();
        foreach (Vector2Int prevTarget in activateTargets)
        {
            Block prevTargetBlock = block.map.GetBlock(prevTarget.x, prevTarget.y);

            if (prevTargetBlock != null)
            {
                Debug.Assert(prevTargetBlock is IRedstoneActivatable, $"wrong activation target {prevTarget} {prevTargetBlock} was set"); //

                if (!prevTargetBlock.activateSources.Remove(block.LastGridPosition))
                    prevTargetBlock.activateSources.Remove(block.GridPosition);

                // add to redstone manager update list
                block.map.RedstoneManager.AddUpdatedBlock(prevTargetBlock);
            }
            block.activateTargets.Remove(prevTarget); // possibly the old target became empty grid
        }
        // add new activated targets
        if (block.isInMap && !block.isRemoved && block.isCharged)
        {
            foreach (Block newTargetBlock in block.map.GetAdjacentBlocks(block.GridPosition.x, block.GridPosition.y, true))
            {
                if (newTargetBlock != null && newTargetBlock.isInMap && newTargetBlock is IRedstoneActivatable component && component.CanActivatedBy(block)) // a valid target found
                {
                    block.activateTargets.Add(newTargetBlock.GridPosition);
                    newTargetBlock.activateSources.Add(block.GridPosition);

                    // add to redstone manager update list
                    block.map.RedstoneManager.AddUpdatedBlock(newTargetBlock);
                }
            }
        }
    }
}