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
    private Vector2 lastPosition = Vector2.zero;

    // Block properties
    public virtual bool IsDummy => false;
    public virtual bool IsFluid => false;

    // Block state flags
    public bool isInMap = false;        // is in the map data
    public bool isLocked = false;       // is locked => ready to be cleared
    public bool isAnimating = false;    // is moving with animation
    public bool isRemoved = false;

    public bool isActivated = false;
    public virtual bool isCharged { get; private set; }

    // Events
    public delegate void OnChangedEvent(Block block);
    public event OnChangedEvent OnBlockUpdated;
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

    public void SetPosition(int x, int y, bool animation = false)
    {
        SetPosition((float)x, (float)y, animation);
    }

    public virtual void OnSpawn(MapManager map)
    {
        this.map = map;
        this.transform.SetParent(map.transform);

        isCharged = false;

        OnRemoved += OnGridUpdated;
        OnMoved += OnGridUpdated;
        OnStateChanged += OnBlockUpdated;
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
                Block.UpdateActivationState(block); // for oncharge no need to foreach
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

    protected virtual void OnGridUpdated(Block block)
    {
        UpdateActivationState(block);
        UpdateChargingState(block);
        OnBlockUpdated?.Invoke(block);
    }

    protected void OnTriggerBlockUpdate()
    {
        OnBlockUpdated?.Invoke(this);
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

    [SerializeField] private List<Vector2Int> activateSources = new List<Vector2Int>();
    [SerializeField] private List<Vector2Int> activateTargets= new List<Vector2Int>();
    [SerializeField] private List<Vector2Int> chargeSources = new List<Vector2Int>();
    [SerializeField] private List<Vector2Int> chargeTargets = new List<Vector2Int>();

    private static void UpdateActivationState(Block block)
    {
        if (block is not IRedstoneActivatable component)
            return;

        // detect new sources
        int pointer = 0;
        foreach (Block adjacent in block.map.GetAdjacentBlocks(block.GridPosition.x, block.GridPosition.y, true))
        {
            if (adjacent != null && adjacent.isCharged)
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

        // activate or deactivate
        if (block.isActivated && block.activateSources.Count == 0)
        {
            block.isActivated = false;
            component.OnRedstoneDeactivated();
        }
        if (!block.isActivated && block.activateSources.Count > 0)
        {
            block.isActivated = true;
            component.OnRedstoneActivated();
        }
    }
    private static void UpdateChargingState(Block block)
    {
        // change charging state



        // update activation
        List<Block> updatedBlocks = new List<Block>();
        // remove old targets
        Vector2Int[] activateTargets = block.activateTargets.ToArray();
        foreach (Vector2Int prevTarget in activateTargets)
        {
            Block prevTargetBlock = block.map.GetBlock(prevTarget.x, prevTarget.y);

            Debug.Assert(prevTargetBlock is IRedstoneActivatable, "wrong activation target was set"); //

            if (prevTargetBlock != null)
            {
                if (!prevTargetBlock.activateSources.Remove(block.LastGridPosition))
                    prevTargetBlock.activateSources.Remove(block.GridPosition);

                if (!updatedBlocks.Contains(prevTargetBlock))
                    updatedBlocks.Add(prevTargetBlock);
            }
            block.activateTargets.Remove(prevTarget); // possibly the old target became empty grid
        }
        // add new activated targets
        if (!block.isRemoved && block.isCharged)
        {
            foreach (Block newTargetBlock in block.map.GetAdjacentBlocks(block.GridPosition.x, block.GridPosition.y, true))
            {
                if (newTargetBlock != null && newTargetBlock is IRedstoneActivatable)
                {
                    block.activateTargets.Add(newTargetBlock.GridPosition);
                    newTargetBlock.activateSources.Add(block.GridPosition);

                    if (!updatedBlocks.Contains(newTargetBlock))
                        updatedBlocks.Add(newTargetBlock);
                }
            }
        }
        // activate or deactivate
        foreach (Block updatedBlock in updatedBlocks)
        {
            if (updatedBlock is IRedstoneActivatable component)
            {
                if (updatedBlock.isActivated && updatedBlock.activateSources.Count == 0)
                {
                    updatedBlock.isActivated = false;
                    component.OnRedstoneDeactivated();
                }
                if (!updatedBlock.isActivated && updatedBlock.activateSources.Count > 0)
                {
                    updatedBlock.isActivated = true;
                    component.OnRedstoneActivated();
                }
            }
        }
    }
}