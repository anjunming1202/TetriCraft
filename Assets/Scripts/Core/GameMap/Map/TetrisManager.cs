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
    protected MapManager Map => map;

    [SerializeField] protected MapTetromino fallingTetromino;
    [SerializeField] private TetrominoController tetrominoController;

    [SerializeField] protected GhostTetromino ghostTetromino;

    [SerializeField] private DummyTetromino[] nextTetrominoList = new DummyTetromino[4];
    public List<DummyTetromino> nextTetrominos { get; private set; } = new List<DummyTetromino>();

    [Header("Rendering Data")]
    [SerializeField] private Canvas sceneCanvas;


    // Tetris map data
    protected int boundaryWidth;
    protected int boundaryHeight;

    // Line clear data
    public uint totalClearLineCount { get; private set; }
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

        if (ShouldUseTetrominoController)
            tetrominoController.Initialise();

        fallingTetromino.OnLockdown += OnLockdown;
        fallingTetromino.OnSoftDrop += (MapTetromino tetromino) => OnTetrominoSoftDrop?.Invoke(tetromino);
        fallingTetromino.OnHardDrop += (MapTetromino tetromino) => OnTetrominoHardDrop?.Invoke(tetromino);
    }

    // Override false to skip TetrominoController entirely — placement-driven subclasses move the
    // falling piece themselves and never route through it, so a scene doesn't need the
    // component/PlayerInput to exist at all (the field can stay unassigned).
    protected virtual bool ShouldUseTetrominoController => true;

    public void PrepareNewTetrisMap(int boundaryWidth, int boundaryHeight, PlayerID playerID)
    {
        // Player reference
        this.PlayerID = playerID;

        // Initialise map
        map.PrepareNewMap(boundaryWidth, boundaryHeight + 5, this);
        this.boundaryWidth = boundaryWidth;
        this.boundaryHeight = boundaryHeight;

        // Initialise falling tetromino
        fallingTetromino.InitEmptyTetromino();
        fallingTetromino.Reset();
        if (ShouldUseTetrominoController)
            tetrominoController.Reset(map, fallingTetromino);

        // Initialise ghost tetromino
        if (ShouldUseGhostTetromino)
            ghostTetromino.CreateGhostBlocks();

        // Initialise next tetrominos
        InitNextTetrominoList();

        // Initialise data
        StopUpdating();
        isTurnFinished = false;
        totalClearLineCount = 0;
        combo = 0; 
        isUpdating = false;
    }

    public void CleanUpTetrisMap()
    {
        map.ClearMap();

        if (ShouldUseGhostTetromino)
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
            if (ShouldUseTetrominoController)
                tetrominoController.OnUpdate();

            // Try clear lines
            TryClearLines();

            // Display ghost tetromino
            if (ShouldUseGhostTetromino && ShouldUpdateGhostTetromino)
                UpdateGhostTetromino();

            // Update map
            map.OnUpdate();
        }
    }

    public void StartNewMap()
    {
        ResumeUpdating();
        // Start first turn
        OnNextTurn();
    }

    public bool AnyBlockAnimating()
    {
        foreach (Block block in map.Blocks)
            if (block.isAnimating) return true;
        return false;
    }

    public void StopUpdating()
    {
        isUpdating = false;
        if (ShouldUseTetrominoController)
            tetrominoController.Deactivate();
    }

    public void ResumeUpdating()
    {
        isUpdating = true;
        if (ShouldUseTetrominoController)
            tetrominoController.Activate();
    }

    // Override false to suppress the automatic ghost update, e.g. when a test harness drives
    // ghostTetromino itself to preview a candidate placement.
    protected virtual bool ShouldUpdateGhostTetromino => true;

    // Override false to skip ghostTetromino entirely — CreateGhostBlocks()/ClearAllBlocks() are
    // never called, so a scene doesn't need the GameObject at all.
    protected virtual bool ShouldUseGhostTetromino => true;

    public void TryEndGame()
    {
        if (!CheckGameDead())
            return;

        StopUpdating();
        OnGameDead?.Invoke();
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
        fallingTetromino.SetPositionPending(spawnPosition);
        map.SpawnTetromino(fallingTetromino);
    }

    private Vector2Int GetSpawnPosition()
    {
        // x position
        int x = (boundaryWidth - fallingTetromino.size) / 2;

        // y position        
        int distance = int.MaxValue; // distance tetromino need to move
        for (int r = 0; r < fallingTetromino.size; r++)
            for (int c = 0; c < fallingTetromino.size; c++)
            {
                if (fallingTetromino.shape[r, c] != null)
                {
                    int distance_new = fallingTetromino.LocalToMap(r, c).y - boundaryHeight;
                    if (distance_new < distance)
                        distance = distance_new;
                }
            }
        int y = fallingTetromino.position.y - distance;

        // spawn point position
        return new Vector2Int(x, y);
    }

    protected virtual void OnLockdown()
    {
        foreach(Block block in fallingTetromino.GetComponentsInChildren<Block>())
        {
            map.ReparentBlock(block);
        }

        // Clear line & line-clear data logic
        totalClearLineCount = 0;
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

    protected virtual void OnNextTurn()
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
        int deathline = boundaryHeight;
        bool gameover = !map.CheckRowEmpty(deathline);
        return gameover;
    }

    private void InitNextTetrominoList()
    {
        // Without this, repeated calls append to the stale list instead of replacing it,
        // corrupting the queue order and the piece sequence on episode/game restarts.
        nextTetrominos.Clear();

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
        for (int i = 0; i < map.GridHeight; i++)
        {
            bool successful = TryClearLine(i);
            if (successful)
                newLineCount++;
        }
        if (newLineCount > 0)
        {
            // accumulate count before next tetromino locked
            totalClearLineCount += newLineCount;

            // events
            OnLineClear?.Invoke(this);
            OnLineClearWithInfo?.Invoke(PlayerID, newLineCount, totalClearLineCount, combo);

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
        for (int i = 0; i < map.GridWidth; i++)
        {
            Block block = map.GetBlock(i, row);
            if (block != null)
                map.RequestDestroyBlock(block);
        }
        map.ImmediatelyProcessGridPendingUpdates();

        // move above rows down
        for (int y = row + 1; y < map.GridHeight; y++)
        {
            for (int x = 0; x < map.GridWidth; x++)  // * must from bottom to top
            {
                if (map.CheckEmpty(x, y) || !map.GetBlock(x, y).isLocked)
                    continue;
                if (!map.CheckEmpty(x, y - 1))
                    continue;

                Block block = map.GetBlock(x, y);
                map.RequestMoveBlock(block, x, y - 1);
            }
            map.ImmediatelyProcessGridPendingUpdates(); // update once for each row
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

            fallingTetromino.ShiftPending(0, -1);
            reachedBottom = !fallingTetromino.CheckValid(map);
        }
        fallingTetromino.ShiftPending(0, 1); // step back by 1

        ghostTetromino.Shadow(fallingTetromino);
        ghostTetromino.SetPosition(fallingTetromino.position, map);

        fallingTetromino.SetPositionPending(fallingTetrominoPosition);
    }
}
