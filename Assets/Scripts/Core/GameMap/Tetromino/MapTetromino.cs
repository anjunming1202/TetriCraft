// Four blocks are one tetromino
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

// A tetromino stores 4 blocks, when falling
[Serializable]
public class MapTetromino : Tetromino
{
    // Wallkick data looked up
    public Vector2Int[] Wallkick => wallkick[new Vector2Int(lastRotation, rotation)];

    // State data
    public bool isGrounded = false;     // grounded => lock delay => lockdown
    public bool isLocked = false;       // lockdown

    // Control recorded data
    public uint softDrop = 0;
    public uint hardDrop = 0;

    // Lock Delay
    public static float lockDelay = 0.5f;
    public Coroutine lockDelayCoroutine = null;

    // Event
    public delegate void TetrominoEvent(MapTetromino tetromino);
    public TetrominoEvent OnSoftDrop; // for player controlled drop: accelerate (soft drop)
    public TetrominoEvent OnHardDrop; // for player controlled drop: land (hard drop)
    public Action OnLockdown;

    public override void Reset()
    {
        base.Reset();
        isGrounded = false;
        isLocked = false;
        softDrop = 0;
        hardDrop = 0;
        lockDelayCoroutine = null;
    }

    /// <summary>
    /// Update blocks in the map according to this tetromino data
    /// </summary>
    public void MoveBlocksToPendingPositions(MapManager map, bool animation = true)
    {
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                // block in tetromino
                Block block = shape[r, c];
                if (block == null)
                    continue;

                // block in map at the position should be updated by tetromino
                Vector2Int currPosition = LocalToMap(r, c);
                Block mapBlock = map.GetBlock(currPosition);

                // check for replacing
                if (mapBlock != null)
                {
                    bool replaceable = mapBlock.CanBeReplacedBy(block);
                    if (replaceable)
                        mapBlock.OnReplacedBy(block);
                }

                // special case: deactivate extending pistons
                if (block is Piston piston && piston.isExtending)
                {
                    piston.ForcedDeactivate();
                    piston.isActivated = false;
                }

                // update the block
                map.RequestMoveBlock(block, currPosition.x, currPosition.y);
            }
        map.ImmediatelyProcessGridPendingUpdates();
    }

    public void SetPositionPending(Vector2Int position)
    {
        this.position = position;
    }

    public void ShiftPending(int x, int y)
    {
        position += new Vector2Int(x, y);
    }









    public void Left(MapManager map)
    {
        TryShift(map, - 1, 0);
    }
    public void Right(MapManager map)
    {
        TryShift(map, 1, 0);
    }
    public void Drop(MapManager map)
    {
        bool successful = TryShift(map, 0, -1);
        if (!successful)
        {
            Lockdown(map);
        }
    }
    public void SoftDrop(MapManager map)
    {
        softDrop++;
        OnSoftDrop?.Invoke(this);
        bool successful = TryShift(map, 0, -1);
        if (!successful)
        {
            Ground(map);
        }
    }
    public void HardDrop(MapManager map)
    {
        while (TryShift(map, 0, -1))
        {
            hardDrop++;
        }
        OnHardDrop?.Invoke(this);
        hardDrop = 0;
        Ground(map);
    }
    public void Rotate(MapManager map, bool clockwise = true)
    {
        TryRotate(map, clockwise);
    }


    private bool TryShift(MapManager map, int x, int y)
    {
        ShiftPending(x, y);
        if (!CheckValid(map))
        {
            ShiftPending(-x, -y);
            return false;
        }
        MoveBlocksToPendingPositions(map, true);
        return true;
    }
    private bool TryRotate(MapManager map, bool clockwise = true)
    {
        RotateShape(clockwise);
        // check for each wall kick position
        foreach (Vector2Int kick in Wallkick)
        {
            ShiftPending(kick.x, kick.y);
            if (CheckValid(map))
            {
                MoveBlocksToPendingPositions(map, true);
                return true;
            }
            ShiftPending(-kick.x, -kick.y);
        }
        RotateShape(!clockwise);
        return false;
    }
    public bool TryImmediateLockdown(MapManager map)
    {
        ShiftPending(0, -1);
        bool canLockdown = !CheckValid(map);
        ShiftPending(0, 1);

        if (canLockdown)
        {
            Lockdown(map);
        }
        return canLockdown;
    }

    /// <summary>
    /// Tetromino grounding
    /// </summary>
    private void Ground(MapManager map)
    {
        // make sure only ground once *
        if (isGrounded)
            return;

        // Grounding
        isGrounded = true;

        // Lock delay => lockdown
        lockDelayCoroutine = StartCoroutine(DelayedLockOnSet(map, lockDelay));
    }
    /// <summary>
    /// Tetromino lockdown
    /// </summary>
    private void Lockdown(MapManager map)
    {
        // make sure only lockdown once *
        if (isLocked)
            return;

        // stop lock delay
        if (lockDelayCoroutine != null)
            StopCoroutine(lockDelayCoroutine);

        // update tetromino & blocks data
        isLocked = true;
        foreach (var block in blocks)
        {
            if (block == null) // block that is destroyed during falling
                continue;

            block.OnLockdown();
        }

        // invoke map tetromino landing event
        OnLockdown?.Invoke();
    }
    private IEnumerator DelayedLockOnSet(MapManager map, float delay)
    {
        if (isLocked)
            StopCoroutine(lockDelayCoroutine);
        yield return new WaitForSeconds(delay);
        Lockdown(map);
    }


    // Map checking
    /// <summary>
    /// Check for bottom, left, and right boundaries
    /// </summary>
    public bool CheckInside(MapManager map)
    {
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                if (shape[r, c] != null)
                {
                    Vector2Int blockPos = LocalToMap(r, c);
                    if (!map.CheckInsideBlockGrid(blockPos.x, blockPos.y))
                        return false;
                }
                /*// special case: piston
                if (shape[r, c] is Piston pistonBlock && pistonBlock.isExtending)
                {
                    Vector2Int blockPos = LocalToMap(r, c) + pistonBlock.Facing;
                    if (!map.CheckInside(blockPos.x, blockPos.y))
                        return false;
                }*/
            }
        return true;
    }
    public bool CheckCollide(MapManager map)
    {
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                Block tetrominoBlock = shape[r, c];
                if (tetrominoBlock != null)
                {
                    Vector2Int mapBlockPos = LocalToMap(r, c);
                    Block mapBlock = map.GetBlock(mapBlockPos);
                    if (mapBlock != null && mapBlock.isLocked)
                    {
                        // if can be replaced => ignore
                        bool collide = !mapBlock.CanBeReplacedBy(tetrominoBlock);
                        if (!collide)
                            continue;

                        Debug.Log($"COLLIDING {position} {mapBlock} {mapBlock.isLocked}");
                        return true;
                    }
                }
                /*// special case: piston
                if (tetrominoBlock is Piston pistonBlock && pistonBlock.isExtending)
                {
                    Vector2Int mapBlockPos = LocalToMap(r, c) + pistonBlock.Facing;
                    Block mapBlock = map[mapBlockPos.x, mapBlockPos.y];
                    if (mapBlock != null && mapBlock.isLocked)
                    {
                        // if can be replaced => ignore
                        bool collide = !mapBlock.CanBeReplacedBy(tetrominoBlock);
                        if (!collide)
                            continue;

                        return true;
                    }
                }*/
            }
        return false;
    }
    public bool CheckValid(MapManager map)
    {
        return !CheckEmpty() && CheckInside(map) && !CheckCollide(map);
    }
}