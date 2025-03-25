using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

// Game Manager for managing the whole game
public class GameManager : MonoBehaviour
{
    // Map Manager
    private MapManager mapManager;
    private Map map;

    // Map Region
    [Header("Map")]
    public SpriteMask boundaryRegion;

    // Map Data (readonly)
    private int width => MapBoundaryData.Instance.width;
    private int height => MapBoundaryData.Instance.height;
    private MapBoundaryData boundary => MapBoundaryData.Instance;

    // Tetrominos
    private TetrominoManager fallingTetromino;
    public TetrominoManager nextTetromino;

    // Timer
    private float timer = 0;

    // Control
    [Header("Control")]
    private bool isAccelerating = false;
    public float speedDrop = 1;
    public float speedSoftDrop = 2;
    private float interval;
    private float intervalNormal => 1 / speedDrop;
    private float intervalAccelerating => 1 / speedSoftDrop;
    public static float SpeedDrop => Instance.speedDrop;
    public static float SpeedSoftDrop => Instance.speedDrop;

    [Header("Input")]
    // Input Key Mapping
    private KeyCode key_left = KeyCode.A;
    private KeyCode key_right = KeyCode.D;
    private KeyCode key_accelerate = KeyCode.S;
    private KeyCode key_land = KeyCode.Space;
    private KeyCode key_rotateCW = KeyCode.E;
    private KeyCode key_rotateCCW = KeyCode.Q;

    [Header("Game Logic")]
    [SerializeField, ReadOnly] private bool gameover = false; // Game over flag
    [SerializeField, ReadOnly] private int score = 0;

    // Visual
    [Header("Visual")]
    public AnimationCurveAsset blockMovementCurve;
    public AnimationCurveAsset blockLandCurve;

    public static GameManager Instance { get; private set; }

    void Awake()
    {
        // Load static resources
        InitialiseResources();

        // Singleton
        Instance = this;

        //
        fallingTetromino = GameObject.Find("Falling Tetromino").GetComponent<TetrominoManager>();
        nextTetromino = GameObject.Find("Next Tetromino").GetComponent<TetrominoManager>();
    }
    public void NewGame()
    {
        // Initialise map
        mapManager = FindObjectOfType<MapManager>();
        mapManager.NewMap();

        map = mapManager.map;

        mapManager.OnFinishTurn += CheckGameover;
        mapManager.OnLineClear += ScoreLineClear;

        fallingTetromino.OnTetrominoSoftDrop += ScoreSoftDrop;
        fallingTetromino.OnTetrominoHardDrop += ScoreHardDrop;
        fallingTetromino.OnLockdown += OnNextTurn;

        // Initialise game logic
        gameover = false;
        score = 0;
        interval = intervalNormal;

        // Spawn the first tetromino
        nextTetromino = TetrominoSpawner.Instance.NewRandomTetromino();
        OnNextTurn();
    }

    void Start()
    {
        // New game
        NewGame();
    }

    ////////////////////////////////////////////////////
    void Update()
    {
        // debug
        int landedCount = 0;
        landedCount = BlockSpawner.Instance.GetComponentsInChildren<Transform>().Length - 1;      
        Debug.Log($"blocks in map: {mapManager.blockCount}, blocks instantiated: {landedCount}");

        if (!gameover)
        {
            // Update tetromino control
            UpdateControl(mapManager);

            // Try clear lines
            mapManager.TryClearLines();
        }
    }


    //================================//
    // Map Control Logic
    //================================//

    private void UpdateControl(MapManager mapManager)
    {
        // Timer
        timer += Time.deltaTime;

        // Control
        if (Input.GetMouseButtonDown(0))
        {

        }
        if (Input.GetKeyDown(key_left)) // Left
        {
            fallingTetromino.Left(map);
        }
        if (Input.GetKeyDown(key_right)) // Right
        {
            fallingTetromino.Right(map);
        }
        if (Input.GetKeyDown(key_accelerate)) // Accelerating
        {
            timer = 0;
            if (fallingTetromino.TryImmediateLockdown(map)) // down key => skip delay and lockdown directly
                return;
            fallingTetromino.SoftDrop(map);  // drop immediately
            isAccelerating = true;
            interval = intervalAccelerating;
        }
        if (Input.GetKeyUp(key_accelerate))
        {
            isAccelerating = false;
            interval = intervalNormal;
        }
        if (Input.GetKeyDown(key_land)) // Hard drop
        {
            timer = 0;
            fallingTetromino.HardDrop(map);
        }
        if (Input.GetKeyDown(key_rotateCW)) // Rotate clockwise
        {
            fallingTetromino.Rotate(map, true);
        }
        if (Input.GetKeyDown(key_rotateCCW)) // Rotate anticlockwise
        {
            fallingTetromino.Rotate(map, false);
        }

        // Drop of tetromino
        if (timer >= interval)
        {
            if (isAccelerating)
                fallingTetromino.SoftDrop(map); // Soft drop
            else
                fallingTetromino.Drop(map); // Normal drop
            timer = 0;
        }
    }
    private void OnNextTurn()
    {
        // Stop when game over
        if (gameover)
            return;

        // Set blocks children to the block pool
        BlockSpawner.Instance.Reparent(TetrominoSpawner.Instance.transform);

        // Spawn new tetromino in map
        SpawnTetromino(map, nextTetromino);

        // Create next new tetromino
        nextTetromino = TetrominoSpawner.Instance.NewRandomTetromino();

        // Display next tetromino

    }

    public void SpawnTetromino(Map map, TetrominoManager newTetromino)
    {
        // Initialise next falling tetromino & its blocks
        fallingTetromino = newTetromino;
        InitialiseNewTetromino(newTetromino);

        // Set tetromino to the spawn position
        SetSpawnPosition(map, fallingTetromino);
    }

    private void InitialiseNewTetromino(TetrominoManager tetromino)
    {
        // reset tetromino & map data
        map.lastClearLineCount = 0;
        map.combo = 0;

        tetromino.softDrop = 0;
        tetromino.hardDrop = 0;

        tetromino.isActive = true;
        tetromino.isLocked = false;

        // set up blocks
        foreach (var block in tetromino.tetromino.blocks)
        {
            InitialiseBlock(block);
        }
    }

    private void InitialiseBlock(Block block)
    {
        // block on spawn falling
        block.SpawnFalling();
    }

    private void SetSpawnPosition(Map map, TetrominoManager tetromino)
    {
        // Set tetromino x position
        int x = (boundary.width - fallingTetromino.tetromino.size) / 2;

        // Set tetromino y position        
        int distance = int.MaxValue; // distance tetromino need to move
        for (int r = 0; r < tetromino.tetromino.size; r++)
            for (int c = 0; c < tetromino.tetromino.size; c++)
            {
                if (tetromino.tetromino.shape[r, c] != null)
                {
                    int distance_new = tetromino.tetromino.LocalToMap(r, c).y - boundary.height;
                    if (distance_new < distance)
                        distance = distance_new;
                }
            }

        // Set tetromino to spawn point
        tetromino.SetPosition(x, tetromino.tetromino.position.y - distance);
        tetromino.UpdateMapBlocks(map);
    }



    private void ScoreSoftDrop(TetrominoManager tetromino)
    {
        score += 1;
    }
    private void ScoreHardDrop(TetrominoManager tetromino)
    {
        score += tetromino.hardDrop * 2;
    }
    private void ScoreLineClear(Map map)
    {
        // for clearing multiple lines
        switch (map.lastClearLineCount)
        {
            case 0:
                break;
            case 1:
                score += 400;
                break;
            case 2:
                score += 1000;
                break;
            case 3:
                score += 2500;
                break;
            case 4:
                score += 8000;
                break;
        }
        // for combo of clearing
        score += map.combo * 500;
    }

    private void CheckGameover(Map map)
    {
        if (map.CheckMapFull())
        {
            gameover = true;
            Debug.Log("Game Over!");
        }
    }



    //================================//
    // Game Resources
    //================================//

    /// <summary>
    /// Initialising all static data/resources
    /// </summary>
    private void InitialiseResources()
    {
        // Initialise boundary data
        MapBoundaryData.Create(boundaryRegion.transform);

        // Initialise block animator
        BlockAnimator.MovingCurveAsset = blockMovementCurve;
        BlockAnimator.LandingCurveAsset = blockLandCurve;
    }
}
