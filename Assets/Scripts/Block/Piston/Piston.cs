using System;
using System.Collections.Generic;
using UnityEngine;

public class Piston : GeneralBlock, IRedstoneActivatable
{
    public override bool IsOriented => true;
    public Vector2Int forwardPosition => GridPosition + Facing;

    public bool isExtending = false;

    public int maxPushNumber = 14;

    public event Action OnExtending;
    public event Action OnContracting;

    public bool OnRedstoneActivated()
    {
        // try extend
        return OnExtend();
    }

    public bool OnRedstoneDeactivated()
    {
        return OnContract();
    }

    public bool CanActivatedBy(Block source)
    {
        // front is not valid
        Vector2Int sourceFace = source.GridPosition - GridPosition;
        if (sourceFace == Facing)
            return false;

        return true;
    }

    private PistonHead pistonHead;

    /*private void Update()
    {
        if (!isInMap || !isLocked)
            return;

        if (Input.GetMouseButtonDown(0))
            OnExtend();
        if (Input.GetMouseButtonDown(1))
            OnContract();
        if (Input.GetKeyDown(KeyCode.R))
            Rotate(true);
    }*/

    private List<Block> pushedBlockList = new List<Block>();

    private bool OnExtend()
    {
        if (isExtending)
        {
            Debug.LogError("piston already extended");
            return false;
        }

        // check whether it's able to push
        pushedBlockList.Clear();

        bool successful = true;
        if (map.IsBlocked(forwardPosition.x, forwardPosition.y))
        {
            Block forwardBlock = map.GetBlock(forwardPosition.x, forwardPosition.y);
            successful = TryPushBlock(forwardBlock, Facing);
        }

        // execute
        if (successful)
        {
            // set pushed blocks
            foreach (Block block in pushedBlockList)
            {
                Vector2Int targetPosition = block.GridPosition + Facing;
                block.SetPosition(targetPosition.x, targetPosition.y, true);
            }
            map.BatchUpdateBlocks();

            // set states
            isExtending = true;

            // set piston head block
            pistonHead = BlockSpawner.NewBlock(BlockID.PistonHead) as PistonHead;
            pistonHead.Init(this);
            pistonHead.OnRemoved += OnHeadRemoved;
            pistonHead.OnDestroyed += OnHeadDestroyed;
            map.SpawnBlock(pistonHead, forwardPosition.x, forwardPosition.y);

            // rendering and sound
            OnExtending?.Invoke();
        }

        return successful;
    }

    private bool OnContract()
    {
        if (!isExtending)
        {
            Debug.LogError("piston hasn't extended");
            return false;
        }

        // set state
        isExtending = false;

        // take back piston head block
        map.RemoveBlock(pistonHead);

        // rendering and sound
        OnContracting?.Invoke();

        return true;
    }

    private bool TryPushBlock(Block block, Vector2Int direction)
    {
        if (block == null || !block.IsPushable || pushedBlockList.Count > maxPushNumber) // null means boundary
            return false; 

        pushedBlockList.Add(block);

        Vector2Int nextPosition = block.GridPosition + Facing;
        if (!map.IsBlocked(nextPosition.x, nextPosition.y))
            return true;

        Block forwardBlock = map.GetBlock(nextPosition.x, nextPosition.y);
        return TryPushBlock(forwardBlock, Facing);
    }

    private void OnHeadRemoved(Block block)
    {
        // if is extending: whole piston is removed
        // if not extending: is taking back the piston head for contraction
        if (isExtending)
        {
            if (!isRemoved)
                map.RemoveBlock(this);
        }
    }

    private void OnHeadDestroyed()
    {
        if (!isRemoved)
            map.DestroyBlock(this);
    }
}
