using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using static Unity.Collections.AllocatorManager;
using static UnityEditor.PlayerSettings;
using static UnityEngine.GraphicsBuffer;

/*// Control the lifecycles of data in the map;
// Control logic of data (How but not When)
//      Control of the tetromino
//      Control map to e.g. clear one row, spawn tetrominos, ... 
//      ...*/
public class MapManager : MonoBehaviour
{
    // Map Data
    [SerializeField] private Map map;

    [SerializeField] private MapTetromino fallingTetromino;
    [SerializeField] private TetrominoController controller;

    [SerializeField] private DummyTetromino ghostTetromino;

    [SerializeField] private DummyTetromino nextTetromino;

    // Line clear data
    public uint lastClearLineCount = 0;
    public uint combo = 0;

    // Updating
    public bool isUpdating = false;

    // Readonly Data
    public int blockCount => map.blockCount;    // debug

    // Events
    public delegate void MapEvent(MapManager mapManager);
    public MapEvent OnLineClear;
    public Action OnFinishTurn;
    public MapTetromino.TetrominoEvent OnTetrominoSoftDrop;
    public MapTetromino.TetrominoEvent OnTetrominoHardDrop;



    private void Awake()
    {
        fallingTetromino.OnLockdown += OnLockdown;
        fallingTetromino.OnSoftDrop += (MapTetromino tetromino) => OnTetrominoSoftDrop?.Invoke(tetromino);
        fallingTetromino.OnHardDrop += (MapTetromino tetromino) => OnTetrominoHardDrop?.Invoke(tetromino);
        OnFinishTurn += OnNextTurn;
    }
    private void Update()
    {
        if (isUpdating)
        {
            // Try clear lines
            TryClearLines();

            // Display ghost tetromino
            SetGhostTetromino();

            // Update blocks
            map.OnUpdateBlocks();
        }
    }

    public void NewMap(int Width, int height)
    {
        // New a map
        map.NewMap(Width, height);

        // Initialise controller
        controller.Initialise(map, fallingTetromino);

        // Initialise data
        isUpdating = false;
        lastClearLineCount = 0;
        combo = 0;
    }

    public void StartUpdating()
    {
        isUpdating = true;
        controller.Activate();
        // Start first turn
        TetrominoGenerator.NewRandomTetromino(nextTetromino);
        OnNextTurn();
    }

    public void FinishUpdating()
    {
        isUpdating = false;
        controller.Deactivate();
    }

    public bool CheckGameover()
    {
        int deathline = map.Height;
        bool gameover = !map.CheckRowEmpty(deathline);
        isUpdating = !gameover;
        return gameover;
    }

    //================================//
    //  Map game logic
    //================================//
    private void SpawnTetromino(Tetromino newTetromino)
    {
        // Initialise next falling tetromino & its blocks
        fallingTetromino.New(newTetromino);

        // Set tetromino to the spawn position
        Vector2Int spawnPosition = GetSpawnPosition();
        fallingTetromino.SetPosition(spawnPosition);
        map.SpawnTetromino(fallingTetromino);
    }

    private Vector2Int GetSpawnPosition()
    {
        // x position
        int x = (map.Width - fallingTetromino.size) / 2;

        // y position        
        int distance = int.MaxValue; // distance tetromino need to move
        for (int r = 0; r < fallingTetromino.size; r++)
            for (int c = 0; c < fallingTetromino.size; c++)
            {
                if (fallingTetromino.shape[r, c] != null)
                {
                    int distance_new = fallingTetromino.LocalToMap(r, c).y - map.Height;
                    if (distance_new < distance)
                        distance = distance_new;
                }
            }
        int y = fallingTetromino.position.y - distance;

        // spawn point position
        return new Vector2Int(x, y);
    }

    private void OnLockdown()
    {
        foreach(Block block in fallingTetromino.GetComponentsInChildren<Block>())
        {
            block.transform.SetParent(map.transform, true);
        }

        // Clear line & line-clear data logic
        lastClearLineCount = 0;
        bool successfulClear = TryClearLines();
        if (successfulClear)
        {
            combo++;
        }
        else
        {
            combo = 0;
        }

        // Finish this turn
        OnFinishTurn?.Invoke();
    }

    private void OnNextTurn()
    {
        // Stop when game over
        if (!isUpdating)
            return;

        // Spawn new tetromino in map
        SpawnTetromino(nextTetromino);

        // Create next new tetromino
        TetrominoGenerator.NewRandomTetromino(nextTetromino);

        // Display next tetromino
        nextTetromino.Display();
    }

    /// <summary>
    /// Try clear line for tetromino when landing
    /// </summary>
    private bool TryClearLines()
    {
        uint newLineCount = 0;
        for (int i = 0; i < map.Height; i++)
        {
            bool successful = TryClearLine(i);
            if (successful)
                newLineCount++;
        }
        if (newLineCount > 0)
        {
            lastClearLineCount += newLineCount;
            OnLineClear?.Invoke(this);
            return true;
        }
        return false;
    }
    private bool TryClearLine(int row)
    {
        if (map.CheckRowFull(row))
        {
            ClearLine(row);
            return true;
        }
        return false;
    }
    private void ClearLine(int row)
    {
        // clear row
        for (int i = 0; i < map.Width; i++)
        {
            map.DestroyBlock(map[i, row]);
        }
        // move above rows down
        for (int y = row + 1; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)  // * must from bottom to top
            {
                if (map.CheckEmpty(x, y) || !map[x, y].isLocked)
                    continue;
                if (!map.CheckEmpty(x, y - 1))
                    continue;
                map[x, y].SetPosition(x, y - 1, true);
            }
            map.BatchUpdateBlocks(); // update once for each row
        }
    }

    private void SetGhostTetromino()
    {
        if (fallingTetromino.type == TetrominoType.None)
        {
            Debug.LogError("missing falling tetromino");
            return;
        }

        bool reachedBottom = false;
        Vector2Int fallingTetrominoPosition = fallingTetromino.position;
        int iter = 0;
        while (!reachedBottom)
        {
            iter++;
            Debug.Assert(iter < 10000, "infinite while");

            fallingTetromino.Shift(0, -1);
            reachedBottom = !fallingTetromino.CheckValid(map);
        }

        ghostTetromino.Transform(fallingTetromino);
        ghostTetromino.SetPosition(fallingTetromino.position);

        fallingTetromino.SetPosition(fallingTetrominoPosition);
    }
}
