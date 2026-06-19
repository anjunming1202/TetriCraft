using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piston : GeneralBlock, IRedstoneActivatable
{
    public override bool IsOriented => true;
    public Vector2Int forwardPosition => GridPosition + Facing;

    public bool isExtending = false;

    public float delay = 0.1f;
    public int maxPushNumber = 12;

    public event Action OnExtending;
    public event Action OnContracting;

    private PistonHead pistonHead;

    private static readonly List<BlockID> destroyableBlocks = new(){
        BlockID.Leaf
    };

    /// <summary>
    /// every block in the pushed block list structure should be listened when NC updates for attempting re-extending
    /// </summary>
    private readonly List<Block> pushedBlockList = new List<Block>();
    private readonly List<Block> destroyedBlockList = new List<Block>();

    private Coroutine redstoneCoroutine;

    void IRedstoneActivatable.OnRedstoneActivated()
    {
        if (redstoneCoroutine != null)
        {
            StopCoroutine(redstoneCoroutine);
            redstoneCoroutine = null;
            return;
        }
        // try extend
        if (this != null)
            redstoneCoroutine = StartCoroutine(DelayExecute(TryExtend, delay));
    }

    void IRedstoneActivatable.OnRedstoneDeactivated()
    {
        if (redstoneCoroutine != null)
        {
            StopCoroutine(redstoneCoroutine);
            redstoneCoroutine = null;
            return;
        }

        if (this != null)
            redstoneCoroutine = StartCoroutine(DelayExecute(TryRetract, delay));
    }

    bool IRedstoneActivatable.CanReceivePowerFrom(Block source)
    {
        // front face is not a valid activation direction
        return source.GridPosition - GridPosition != Facing;
    }

    public override void SetGridPosition(int x, int y, bool animation = false)
    {
        base.SetGridPosition(x, y, animation);

        if (isExtending)
            pistonHead.SetGridPosition(x + Facing.x, y + Facing.y, animation);
    }

    public override void OnRequestingDestroy()
    {
        base.OnRequestingDestroy();
        ClearPushedBlockList(); // desubscribe NC listeners
        if (isExtending && pistonHead != null)
        {
            isExtending = false;
            map.RequestDestroyBlock(pistonHead);
        }
    }

    public override void OnRequestingRemove()
    {
        base.OnRequestingRemove();
        ClearPushedBlockList(); // desubscribe NC listeners
        if (isExtending && pistonHead != null)
        {
            isExtending = false;
            map.RequestRemoveBlock(pistonHead);
        }
    }

    public override void OnNCUpdateRespond(Vector2Int updateSrc)
    {
        if (isRemoved) return;
        base.OnNCUpdateRespond(updateSrc);
        if (!isExtending && redstoneCoroutine == null && map.RedstoneManager.IsActivated(this))
            redstoneCoroutine = StartCoroutine(DelayExecute(TryExtend, delay));
    }

    /// <summary>
    /// force the piston to deactivate
    /// </summary>
    public void ForcedDeactivate()
    {
        if (redstoneCoroutine != null)
        {
            StopCoroutine(redstoneCoroutine);
        }
        TryRetract();
        redstoneCoroutine = null;
    }

    private IEnumerator DelayExecute(Action action, float delay)
    {
        if (delay <= 0f)
        {
            action();
            yield break;
        }
        yield return new WaitForSeconds(delay);
        action();
        redstoneCoroutine = null;
    }

    private void TryExtend()
    {
        if (isExtending)
        {
            Debug.LogWarning("piston tried to extend bur already extended");
            return;
        }

        // check whether it's able to push
        ClearPushedBlockList();
        destroyedBlockList.Clear();

        bool successful = true;
        if (map.IsBlockedWithoutCeiling(forwardPosition.x, forwardPosition.y))
        {
            Block forwardBlock = map.GetBlock(forwardPosition.x, forwardPosition.y);
            successful = TryPushBlock(forwardBlock);
        }

        // execute
        if (successful)
        {
            ExecuteExtension();
        }
        // failed to execute => subscribe notifications
        else
        {
            foreach (Block block in pushedBlockList)
            {
                BlockNCUpdateManager.SubscribeNCNotification(block, this);
            }
        }
    }

    private void TryRetract()
    {
        if (!isExtending)
        {
            Debug.LogWarning("piston tried to contract but hadn't extended");
            return;
        }

        // set state
        isExtending = false;

        // take back piston head block, after setting extending false
        map.RequestRemoveBlock(pistonHead);

        // trigger piston NC update
        TriggerSelfNCUpdate();

        // rendering and sound
        OnContracting?.Invoke();
    }

    private void ClearPushedBlockList()
    {
        BlockNCUpdateManager.DesubscribeAllNCNotifications(this);
        pushedBlockList.Clear();
    }

    private bool TryPushBlock(Block block)
    {
        // boundary condition: forward is destroyable => true
        if (block != null && destroyableBlocks.Contains(block.ID))
        {
            destroyedBlockList.Add(block);
            return true;
        }

        // add to pushed block list
        if (block != null && !pushedBlockList.Contains(block))
            pushedBlockList.Add(block);

        // boundary condition: block unable to push (null block means boundary)
        if (block == null || !block.IsPushable || pushedBlockList.Count > maxPushNumber)
            return false;

        // boundary condition special case: extending piston
        if (block is Piston piston && piston.isExtending)
            return TryPushBlock(piston.pistonHead); // false

        // boundary condition: forward is air => true
        Vector2Int nextPosition = block.GridPosition + Facing;
        if (!map.IsBlockedWithoutCeiling(nextPosition.x, nextPosition.y))
            return true;

        // recursion: try push forward block
        Block forwardBlock = map.GetBlock(nextPosition.x, nextPosition.y);
        return TryPushBlock(forwardBlock);
    }

    private void ExecuteExtension()
    {
        // destroy blocks to be destroyed
        for (int i = destroyedBlockList.Count - 1; i >= 0; i--)
        {
            map.RequestDestroyBlock(destroyedBlockList[i]);
        }

        // set pushed blocks
        foreach (Block block in pushedBlockList)
        {
            Vector2Int targetPosition = block.GridPosition + Facing;
            map.RequestMoveBlock(block, targetPosition.x, targetPosition.y, true);
        }

        // update block positions in map
        //map.BatchUpdateBlocks();

        // set states
        isExtending = true;

        // set piston head block
        pistonHead = BlockSpawner.NewBlock(BlockID.PistonHead) as PistonHead;
        pistonHead.Init(this);
        pistonHead.OnPendingRemoved += OnHeadPendingRemoved;
        pistonHead.OnPendingDestroyed += OnHeadPendingDestroyed;
        map.RequestSpawnBlock(pistonHead, forwardPosition.x, forwardPosition.y);

        // flush grid updates immediately so subsequent pistons see the updated state
        map.ImmediatelyProcessGridPendingUpdates();

        // trigger piston NC update
        TriggerSelfNCUpdate();

        // rendering and sound
        OnExtending?.Invoke();
    }

    private void OnHeadPendingRemoved(Block block)
    {
        if (isExtending)
        {
            // external force removed the head: retract state + remove base
            isExtending = false;
            OnContracting?.Invoke();
            map.RequestRemoveBlock(this);
        }
        // else: self-initiated retraction (TryRetract already set state and fired OnContracting)
    }

    private void OnHeadPendingDestroyed(Block block)
    {
        map.RequestDestroyBlock(this);
    }
}
