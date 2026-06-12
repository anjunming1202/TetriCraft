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

    bool IRedstoneActivatable.CanActivatedBy(Block source)
    {
        // front is not valid
        Vector2Int sourceFace = source.GridPosition - GridPosition;
        if (sourceFace == Facing)
            return false;

        return source.isCharged;
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
        //map.RequestDestroyBlock(pistonHead);
    }

    public override void OnRequestingRemove()
    {
        base.OnRequestingRemove();
        //map.RequestRemoveBlock(pistonHead);
    }

    public override void OnNCUpdateRespond(Vector2Int updateSrc)
    {
        base.OnNCUpdateRespond(updateSrc);

        if (isActivated && !isExtending)
        {
            TryExtend();
        }
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

    private PistonHead pistonHead;

    private static readonly List<BlockID> destroyableBlocks = new(){
        BlockID.Leaf
    };

    /// <summary>
    /// every block in the pushed block list structure should be listened when NC updates for attempting re-extending
    /// </summary>
    private List<Block> pushedBlockList = new List<Block>();
    private List<Block> destroyedBlockList = new List<Block>();

    private Coroutine redstoneCoroutine;
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
        ResetPushedBlockList();
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

        // take back piston head block
        map.RequestRemoveBlock(pistonHead);

        // rendering and sound
        OnContracting?.Invoke();
    }

    private void ResetPushedBlockList()
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

    private bool CheckForwardAir(Block block)
    {
        Vector2Int nextPosition = block.GridPosition + Facing;
        if (!map.IsBlockedWithoutCeiling(nextPosition.x, nextPosition.y))
            return true;
        return false;
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
            block.SetGridPosition(targetPosition.x, targetPosition.y, true);
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

        // trigger piston NC update
        map.RequestNCUpdate(GridPosition);

        // rendering and sound
        OnExtending?.Invoke();
    }

    private void OnHeadPendingRemoved(Block block)
    {
        // if is extending: whole piston is removed
        // if not extending: is taking back the piston head for contraction
        if (isExtending)
        {
            //map.RequestRemoveBlock(this);
        }
    }

    private void OnHeadPendingDestroyed(Block block)
    {
        //map.RequestDestroyBlock(this);
    }
}
