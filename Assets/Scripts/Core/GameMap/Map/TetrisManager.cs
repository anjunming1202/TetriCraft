using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*// Control the lifecycles of data in the map;
// Control logic of data (How but not When)
//      Control of the tetromino
//      Control map to e.g. clear one row, spawn tetrominos, ... 
//      ...*/
public class TetrisManager : MonoBehaviour
{
    public PlayerID PlayerID { get; private set; }

    [Header("Map Objects")]
    [SerializeField] private MapManager map;

    [SerializeField] private MapTetromino fallingTetromino;
    [SerializeField] private TetrominoController tetrominoController;

    [SerializeField] private GhostTetromino ghostTetromino;

    [SerializeField] private DummyTetromino[] nextTetrominoList = new DummyTetromino[4];
    public List<DummyTetromino> nextTetrominos { get; private set; } = new List<DummyTetromino>();

    [Header("Rendering Data")]
    [SerializeField] private Canvas sceneCanvas;


    // Line clear data
    public uint lastClearLineCount { get; private set; }
    public uint combo { get; private set; }

    // Updating state
    public bool isUpdating { get; private set; }

    // Tetris map states
    private bool isTurnFinished;
    public event Action OnFinishedTurn;
    public event Action OnStartedTurn;

    // Events
    public event Action OnTetrominoListInitialised;
    public event Action OnGameDead;

    public event Action<TetrisManager> OnLineClear;
    public event Action<PlayerID, uint, uint, uint> OnLineClearWithInfo; // player id, new line count, total line count, combo, (clear type)
    public event MapTetromino.TetrominoEvent OnTetrominoSoftDrop;
    public event MapTetromino.TetrominoEvent OnTetrominoHardDrop;



    public void Initialise()
    {
        map.Initialise();

        tetrominoController.Initialise();

        fallingTetromino.OnLockdown += OnLockdown;
        fallingTetromino.OnSoftDrop += (MapTetromino tetromino) => OnTetrominoSoftDrop?.Invoke(tetromino);
        fallingTetromino.OnHardDrop += (MapTetromino tetromino) => OnTetrominoHardDrop?.Invoke(tetromino);
    }

    public void PrepareNewTetrisMap(int Width, int height, PlayerID playerID)
    {
        // Player reference
        this.PlayerID = playerID;

        // Initialise map
        map.PrepareNewMap(Width, height, this);

        // Initialise falling tetromino
        fallingTetromino.InitEmptyTetromino();
        fallingTetromino.Reset();
        tetrominoController.Reset(map, fallingTetromino);

        // Initialise ghost tetromino
        ghostTetromino.CreateGhostBlocks();

        // Initialise next tetrominos
        InitNextTetrominoList();

        // Initialise data
        StopUpdating();
        isTurnFinished = false;
        lastClearLineCount = 0;
        combo = 0; 
        isUpdating = false;
    }

    public void CleanUpTetrisMap()
    {
        map.ClearMap();

        ghostTetromino.ClearAllBlocks();
        fallingTetromino.ClearAllBlocks();
        foreach (var tetromino in nextTetrominoList)
            tetromino.ClearAllBlocks();
    }

    public void OnUpdate()
    {
        if (isUpdating)
        {
            // Check game is not dead
            TryEndGame();

            // Update turn
            if (isTurnFinished)
            {
                OnNextTurn();
                isTurnFinished = false;
            }

            // Update tetromino
            tetrominoController.OnUpdate();

            // Try clear lines
            TryClearLines();

            // Display ghost tetromino
            UpdateGhostTetromino();

            // Update map
            TickManager.Update();
            map.OnUpdate();
        }
    }

    public void StartNewMap()
    {
        ResumeUpdating();
        // Start first turn
        OnNextTurn();
    }

    public void StopUpdating()
    {
        isUpdating = false;
        tetrominoController.Deactivate();
    }

    public void ResumeUpdating()
    {
        isUpdating = true;
        tetrominoController.Activate();
    }

    public void TryEndGame()
    {
        if (!CheckGameDead())
            return;

        StopUpdating();
        OnGameDead?.Invoke();
    }

    public void QueueGarbage(int lines)
    {

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
        int x = (map.BoundaryWidth - fallingTetromino.size) / 2;

        // y position        
        int distance = int.MaxValue; // distance tetromino need to move
        for (int r = 0; r < fallingTetromino.size; r++)
            for (int c = 0; c < fallingTetromino.size; c++)
            {
                if (fallingTetromino.shape[r, c] != null)
                {
                    int distance_new = fallingTetromino.LocalToMap(r, c).y - map.BoundaryHeight;
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
            map.ReparentBlock(block);
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

        // Turn state to finish
        isTurnFinished = true;
        OnFinishedTurn?.Invoke();
    }

    private void OnNextTurn()
    {
        // Stop when game over
        if (!isUpdating)
            return;

        // Spawn new tetromino in map
        DummyTetromino tetrominoSpawned = nextTetrominos[0];
        SpawnTetromino(tetrominoSpawned);

        // Create next new tetromino
        nextTetrominos.RemoveAt(0);
        nextTetrominos.Add(tetrominoSpawned);
        TetrominoGenerator.NewRandomTetromino(tetrominoSpawned);

        // Turn state to start
        isTurnFinished = false;
        OnStartedTurn?.Invoke();
    }

    private bool CheckGameDead()
    {
        int deathline = map.BoundaryHeight;
        bool gameover = !map.CheckRowEmpty(deathline);
        return gameover;
    }

    private void InitNextTetrominoList()
    {
        foreach (var tetromino in nextTetrominoList)
        {
            TetrominoGenerator.NewRandomTetromino(tetromino);
            nextTetrominos.Add(tetromino);
            tetromino.gameObject.SetActive(false);
        }

        OnTetrominoListInitialised?.Invoke();
    }

    /// <summary>
    /// Try clear line for tetromino when landing
    /// </summary>
    private bool TryClearLines()
    {
        uint newLineCount = 0;
        for (int i = 0; i < map.BoundaryHeight; i++)
        {
            bool successful = TryClearLine(i);
            if (successful)
                newLineCount++;
        }
        if (newLineCount > 0)
        {
            // accumulate count before next tetromino locked
            lastClearLineCount += newLineCount;

            // events
            OnLineClear?.Invoke(this);
            OnLineClearWithInfo?.Invoke(PlayerID, newLineCount, lastClearLineCount, combo);

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
        for (int i = 0; i < map.BoundaryWidth; i++)
        {
            Block block = map.GetBlock(i, row);
            if (block != null)
                map.DestroyBlock(block);
        }
        // move above rows down
        for (int y = row + 1; y < map.BoundaryHeight; y++)
        {
            for (int x = 0; x < map.BoundaryWidth; x++)  // * must from bottom to top
            {
                if (map.CheckEmpty(x, y) || !map.GetBlock(x, y).isLocked)
                    continue;
                if (!map.CheckEmpty(x, y - 1))
                    continue;
                map.GetBlock(x, y).SetPosition(x, y - 1, true);
            }
            map.BatchUpdateBlocks(); // update once for each row
        }
    }

    private void UpdateGhostTetromino()
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
        fallingTetromino.Shift(0, 1); // step back by 1

        ghostTetromino.Shadow(fallingTetromino);
        ghostTetromino.SetPosition(fallingTetromino.position, map);

        fallingTetromino.SetPosition(fallingTetrominoPosition);
    }
}
