using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using static Unity.Collections.AllocatorManager;
using static UnityEditor.PlayerSettings;

/*// Control the lifecycles of data in the map;
// Control logic of data (How but not When)
//      Control of the tetromino
//      Control map to e.g. clear one row, spawn tetrominos, ... 
//      ...*/
public class MapManager : MonoBehaviour
{
    // Map Data
    private Map map;

    // Tetromino being controlled
    private Tetromino fallingTetromino;

    // Readonly Data
    private MapBoundaryData boundary => MapBoundaryData.Instance; // Boundary Data
    public int blockCount => map.blockCount;

    // Events
    public Action OnLockdown;
    public delegate void MapEvent(Map map);
    public MapEvent OnFinishTurn;
    public MapEvent OnLineClear;
    public delegate void TetrominoEvent(Tetromino tetromino);
    public TetrominoEvent OnTetrominoSoftDrop; // for player controlled drop: accelerate (soft drop)
    public TetrominoEvent OnTetrominoHardDrop; // for player controlled drop: land (hard drop)



    //================================//
    //  Initialise Map
    //================================//
    public void NewMap()
    {
        // New a map
        map = new Map();
    }



    //================================//
    //  Initialise Tetromino & Blocks
    //================================//
    public void SpawnTetromino(Tetromino newTetromino)
    {
        // Initialise next falling tetromino & its blocks
        fallingTetromino = newTetromino;
        InitialiseNewTetromino(newTetromino);

        // Rotate tetromino randomly

        // Set tetromino to the spawn position
        SetToSpawnPosition(fallingTetromino);

    }


    private void InitialiseNewTetromino(Tetromino tetromino)
    {
        // reset tetromino & map data
        map.lastClearLineCount = 0;
        map.combo = 0;

        tetromino.softDrop = 0;
        tetromino.hardDrop = 0;

        tetromino.isActive = true;
        tetromino.isLocked = false;

        // set up blocks
        foreach (var block in tetromino.blocks)
        {
            InitialiseBlock(block);
        }
    }
    private void InitialiseBlock(Block block)
    {
        // block on spawn falling
        block.SpawnFalling();
    }
    private void SetToSpawnPosition(Tetromino tetromino)
    {
        // Set tetromino x position
        int x = (boundary.width - fallingTetromino.size) / 2;

        // Set tetromino y position        
        int distance = int.MaxValue; // distance tetromino need to move
        for (int r = 0; r < tetromino.size; r++)
            for (int c = 0; c < tetromino.size; c++)
            {
                if (tetromino[r, c] != null)
                {
                    int distance_new = tetromino.LocalToMap(r, c).y - boundary.height;
                    if (distance_new < distance)
                        distance = distance_new;
                }
            }

        // Set tetromino to spawn point
        map.SetTetromino(tetromino, new Vector2Int(x, tetromino.MapPosition.y - distance));
    }



    //================================//
    //  Tetromino Control
    //================================//
    public void Left()
    {
        TryMoveBy(fallingTetromino, -1, 0);
    }
    public void Right()
    {
        TryMoveBy(fallingTetromino, 1, 0);
    }
    public void Drop()
    {
        bool successful = TryMoveBy(fallingTetromino, 0, -1);
        if (!successful)
        {
            Lockdown(fallingTetromino);
        }
    }
    public void SoftDrop()
    {
        fallingTetromino.softDrop++;
        OnTetrominoSoftDrop?.Invoke(fallingTetromino);
        bool successful = TryMoveBy(fallingTetromino, 0, -1);
        if (!successful)
        {
            Ground(fallingTetromino);
        }
    }
    public void HardDrop()
    {
        while (TryMoveBy(fallingTetromino, 0, -1))
        {
            fallingTetromino.hardDrop++;
        }
        OnTetrominoHardDrop?.Invoke(fallingTetromino);
        fallingTetromino.hardDrop = 0;
        Ground(fallingTetromino);
    }
    public void Rotate(bool clockwise = true)
    {
        TryRotate(fallingTetromino, clockwise);
    }
    
    public bool TryImmediateLockdown()
    {
        fallingTetromino.MoveBy(0, -1);
        bool canLockdown = !map.CheckValid(fallingTetromino);
        fallingTetromino.MoveBy(0, 1);

        if (canLockdown)
        {
            Lockdown(fallingTetromino);
        }
        return canLockdown;
    }


    private bool TryMoveBy(Tetromino tetromino, int x, int y)
    {
        tetromino.MoveBy(x, y);
        if (!map.CheckValid(tetromino))
        {
            tetromino.MoveBy(-x, -y);
            return false;
        }
        map.MoveTetrominoTo(tetromino, tetromino.MapPosition);
        return true;
    }
    private bool TryRotate(Tetromino tetromino, bool clockwise = true)
    {
        tetromino.Rotate(clockwise);
        // check for each wall kick position
        foreach (Vector2Int kick in tetromino.Wallkick())
        {
            tetromino.MoveBy(kick.x, kick.y);
            if (map.CheckValid(tetromino))
            {
                map.MoveTetrominoTo(tetromino, tetromino.MapPosition);
                Debug.Log("success rotation");
                return true;
            }
            tetromino.MoveBy(-kick.x, -kick.y);
        }
        tetromino.Rotate(!clockwise);
        Debug.Log("fail rotation");
        return false;
    }

    /// <summary>
    /// Tetromino grounding
    /// </summary>
    private void Ground(Tetromino tetromino)
    {
        // make sure only ground once *
        if (tetromino.isGrounded)
            return;

        // Grounding
        tetromino.isGrounded = true;

        // Lock delay => lockdown
        tetromino.lockDelayCoroutine = StartCoroutine(DelayedLockOnSet(tetromino, tetromino.lockDelay));
    }
    /// <summary>
    /// Tetromino lockdown
    /// </summary>
    private void Lockdown(Tetromino tetromino)
    {
        // make sure only lockdown once *
        if (tetromino.isLocked)
            return;

        // stop lock delay
        if (tetromino.lockDelayCoroutine != null)
            StopCoroutine(tetromino.lockDelayCoroutine);

        // update tetromino & blocks data
        tetromino.Lockdown();

        // invoke map tetromino landing event
        OnLockdown?.Invoke();
    }
    private IEnumerator DelayedLockOnSet(Tetromino tetromino, float delay)
    {
        if (tetromino.isLocked)
            StopCoroutine(tetromino.lockDelayCoroutine);
        yield return new WaitForSeconds(delay);
        Lockdown(tetromino);
    }



    //================================//
    //  Line Clear
    //================================//
    /// <summary>
    /// Try clear line for tetromino when landing
    /// </summary>
    public void TryClearLines()
    {
        int lineCount = 0;
        for (int i = 0; i < map.height; i++)
        {
            bool successful = TryClearLine(i);
            if (successful)
                lineCount++;
        }
        if (lineCount > 0)
        {
            map.lastClearLineCount = lineCount;
            map.combo++;
            OnLineClear?.Invoke(map);
        }
        else
        {
            map.combo = 0;
        }
    }
    private bool TryClearLine(int row)
    {
        if (map.IsRowFull(row))
        {
            ClearLine(row);
            return true;
        }
        return false;
    }
    private void ClearLine(int row)
    {
        // clear row
        map.DestroyLine(row);
        // move above rows down
        for (int x = 0; x < map.width; x++)
            for (int y = row + 1; y < map.height; y++)  // * must from bottom to top
            {
                if (!map.IsEmpty(x, y))
                {
                    map.MoveBlockTo(map[x, y], new Vector2Int(x, y - 1));
                }
            }
    }
}
