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

    // Map Region
    [Header("Map")]
    public SpriteMask boundaryRegion;

    // Map Data (readonly)
    private int width => MapBoundaryData.Instance.width;
    private int height => MapBoundaryData.Instance.height;
    private MapBoundaryData boundary => MapBoundaryData.Instance;

    // Tetrominos
    private Tetromino nextTetromino;
    private TetrominoManager TetrominoManager;

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
    }
    public void NewGame()
    {
        // Initialise map
        mapManager = FindObjectOfType<MapManager>();
        mapManager.NewMap();

        mapManager.OnFinishTurn += CheckGameover;
        mapManager.OnTetrominoSoftDrop += ScoreSoftDrop;
        mapManager.OnTetrominoHardDrop += ScoreHardDrop;
        mapManager.OnLineClear += ScoreLineClear;
        mapManager.OnLockdown += SpawnTetromino;

        // Initialise game logic
        gameover = false;
        score = 0;
        interval = intervalNormal;

        // Spawn the first tetromino
        nextTetromino = TetrominoSpawner.Instance.NewRandomTetromino();
        SpawnTetromino();
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
            // Test
            //SpawnTetromino();
        }
        if (Input.GetKeyDown(key_left)) // Left
        {
            mapManager.Left();
        }
        if (Input.GetKeyDown(key_right)) // Right
        {
            mapManager.Right();
        }
        if (Input.GetKeyDown(key_accelerate)) // Accelerating
        {
            timer = 0;
            if (mapManager.TryImmediateLockdown()) // down key => skip delay and lockdown directly
                return;
            mapManager.SoftDrop();  // drop immediately
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
            mapManager.HardDrop();
        }
        if (Input.GetKeyDown(key_rotateCW)) // Rotate clockwise
        {
            mapManager.Rotate(true);
        }
        if (Input.GetKeyDown(key_rotateCCW)) // Rotate anticlockwise
        {
            mapManager.Rotate(false);
        }

        // Drop of tetromino
        if (timer >= interval)
        {
            if (isAccelerating)
                mapManager.SoftDrop(); // Soft drop
            else
                mapManager.Drop(); // Normal drop
            timer = 0;
        }
    }
    private void SpawnTetromino()
    {
        // Stop when game over
        if (gameover)
            return;

        // Set blocks children to the block pool
        BlockSpawner.Instance.Reparent(TetrominoSpawner.Instance.transform);

        // Spawn new tetromino in map
        mapManager.SpawnTetromino(nextTetromino);

        // Create next new tetromino
        nextTetromino = TetrominoSpawner.Instance.NewRandomTetromino();

        // Display next tetromino

    }



    private void ScoreSoftDrop(Tetromino tetromino)
    {
        score += 1;
    }
    private void ScoreHardDrop(Tetromino tetromino)
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
        if (map.CheckFull())
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
