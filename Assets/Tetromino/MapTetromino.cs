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
    // Map reference
    private Map map;

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
    public TetrominoEvent OnTetrominoSoftDrop; // for player controlled drop: accelerate (soft drop)
    public TetrominoEvent OnTetrominoHardDrop; // for player controlled drop: land (hard drop)
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

    public void SpawnToMap(Map map)
    {
        this.map = map;
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                Block block = shape[r, c];
                if (block == null)
                    continue;
                Vector2Int currPosition = LocalToMap(r, c);
                map.SpawnBlock(block, currPosition.x, currPosition.y);
            }
    }

    /// <summary>
    /// Update blocks in the map according to this tetromino data
    /// </summary>
    public void UpdateBlocks(Map map, bool animation = true)
    {
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                Block block = shape[r, c];
                if (block == null)
                    continue;
                Vector2Int currPosition = LocalToMap(r, c);
                block.SetPosition(currPosition.x, currPosition.y, animation);
            }
        map.BatchUpdateBlocks();
    }

    public Vector2Int LocalToMap(int row, int column)
    {
        return position + new Vector2Int(column, size - 1 - row);
    }

    public void SetPosition(Vector2Int position)
    {
        this.position = position;
    }    

    public void Shift(int x, int y)
    {
        position += new Vector2Int(x, y);
    }









    public void Left()
    {
        TryShift(-1, 0);
    }
    public void Right()
    {
        TryShift(1, 0);
    }
    public void Drop()
    {
        bool successful = TryShift(0, -1);
        if (!successful)
        {
            Lockdown();
        }
    }
    public void SoftDrop()
    {
        softDrop++;
        OnTetrominoSoftDrop?.Invoke(this);
        bool successful = TryShift(0, -1);
        if (!successful)
        {
            Ground();
        }
    }
    public void HardDrop()
    {
        while (TryShift(0, -1))
        {
            hardDrop++;
        }
        OnTetrominoHardDrop?.Invoke(this);
        hardDrop = 0;
        Ground();
    }
    public void Rotate(bool clockwise = true)
    {
        TryRotate(clockwise);
    }


    private bool TryShift(int x, int y)
    {
        Shift(x, y);
        if (!CheckValid(map))
        {
            Shift(-x, -y);
            return false;
        }
        UpdateBlocks(map, true);
        return true;
    }
    private bool TryRotate(bool clockwise = true)
    {
        RotateShape(clockwise);
        // check for each wall kick position
        foreach (Vector2Int kick in Wallkick)
        {
            Shift(kick.x, kick.y);
            if (CheckValid(map))
            {
                UpdateBlocks(map, true);
                Debug.Log("success rotation");
                return true;
            }
            Shift(-kick.x, -kick.y);
        }
        RotateShape(!clockwise);
        Debug.Log("fail rotation");
        return false;
    }
    public bool TryImmediateLockdown()
    {
        Shift(0, -1);
        bool canLockdown = !CheckValid(map);
        Shift(0, 1);

        if (canLockdown)
        {
            Lockdown();
        }
        return canLockdown;
    }

    /// <summary>
    /// Tetromino grounding
    /// </summary>
    private void Ground()
    {
        // make sure only ground once *
        if (isGrounded)
            return;

        // Grounding
        isGrounded = true;

        // Lock delay => lockdown
        lockDelayCoroutine = StartCoroutine(DelayedLockOnSet(lockDelay));
    }
    /// <summary>
    /// Tetromino lockdown
    /// </summary>
    private void Lockdown()
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
            block.Lockdown();
        }

        // reparent blocks
        blocks[0].transform.SetParent(map.transform, true);
        blocks[1].transform.SetParent(map.transform, true);
        blocks[2].transform.SetParent(map.transform, true);
        blocks[3].transform.SetParent(map.transform, true);

        // invoke map tetromino landing event
        OnLockdown?.Invoke();
    }
    private IEnumerator DelayedLockOnSet(float delay)
    {
        if (isLocked)
            StopCoroutine(lockDelayCoroutine);
        yield return new WaitForSeconds(delay);
        Lockdown();
    }


    // Map checking
    /// <summary>
    /// Check for bottom, left, and right boundaries
    /// </summary>
    public bool CheckInside(Map map)
    {
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                if (shape[r, c] != null)
                {
                    Vector2Int blockPos = LocalToMap(r, c);
                    if (!map.CheckInside(blockPos.x, blockPos.y))
                        return false;
                }
            }
        return true;
    }
    public bool CheckCollide(Map map)
    {
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                if (shape[r, c] != null)
                {
                    Vector2Int mapBlockPos = LocalToMap(r, c);
                    Block mapBlock = map[mapBlockPos.x, mapBlockPos.y];
                    if (mapBlock != null && mapBlock.isLocked)
                    {
                        Debug.Log("Collide");
                        return true;
                    }
                }
            }
        return false;
    }
    public bool CheckValid(Map map)
    {
        return (CheckInside(map) && !CheckCollide(map));
    }
}