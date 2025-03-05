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

    // Map region
    [Header("Map")]
    public SpriteMask boundaryRegion;

    // Map Data (readonly)
    private Map map => mapManager.map;
    private int width => MapBoundaryData.Instance.width;
    private int height => MapBoundaryData.Instance.height;
    private MapBoundaryData boundary => MapBoundaryData.Instance;

    // Timer
    private float timer = 0;

    // Control
    [Header("Control")]
    //private bool isAccelerating = false;
    private bool isAccelerating = false;
    public float speedFalling = 1;
    public float speedAcclerating = 2;
    private float interval;
    private float intervalNormal => 1 / speedFalling;
    private float intervalAccelerating => 1 / speedAcclerating;

    // Game Objects Container
    private List<Transform> blockObjects;

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


    void Awake()
    {
        // Load static resources
        InitialiseResources();
    }
    public void NewGame()
    {
        // Initialise map
        mapManager = FindObjectOfType<MapManager>();
        mapManager.NewMap();

        mapManager.OnTetrominoLocked += CheckGameover;
        mapManager.OnTetrominoLocked += SpawnTetromino;
        mapManager.OnTetrominoSoftDrop += ScoreSoftDrop;
        mapManager.OnTetrominoHardDrop += ScoreHardDrop;
        mapManager.OnLineClear += ScoreLineClear;

        // Initialise block object list
        if (blockObjects != null)
        {
            foreach (Transform t in blockObjects)
                GameObject.Destroy(t);
        }
        blockObjects = new List<Transform>();

        // Initialise game logic
        interval = intervalNormal;
        gameover = false;
        score = 0;

        // Spawn the first tetromino
        SpawnTetromino(mapManager);
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
        landedCount = BlockFactory.Blocks.GetComponentsInChildren<Transform>().Length - 1;
        int blockCount = 0;
        foreach (var t in mapManager.map.blockMap)
        {
            if (t != null)
            {
                blockCount++;
            }
        }
        Debug.Log($"blocks in map: {blockCount}, blocks instantiated: {landedCount}");

        if (!gameover)
        {
            // Update tetromino control
            UpdateControl(mapManager);
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
            mapManager.HardDrop();
            timer = 0;
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
    private void SpawnTetromino(MapManager mapManager)
    {
        // Stop when game over
        if (gameover)
            return;

        // Set blocks children to the block pool
        TetrominoFactory.ReparentBlocks();

        // Create new tetromino in map
        mapManager.SpawnTetromino();

        // Instantiate the tetromino/* and keep block objects reference*/
        GameObject tetrominoObject = TetrominoFactory.CreateTetromino(mapManager.CurrentTetromino);
        // InstantiateTetromino(mapManager.CurrentTetromino);
    }

    private void ScoreSoftDrop(MapManager mapManager)
    {
        score += 1;
    }
    private void ScoreHardDrop(MapManager mapManager)
    {
        score += mapManager.hardDrop * 2;
    }
    private void ScoreLineClear(MapManager mapManager)
    {
        // for clearing multiple lines
        switch (mapManager.lineCount)
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
        score += mapManager.combo * 500;
    }

    private void CheckGameover(MapManager mapManager)
    {
        if (mapManager.CheckGameover())
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
        // Load block prefabs
        BlockResourcesManager.RegisterBlocks();

        // Initialise factories
        BlockFactory.Initialise();
        TetrominoFactory.Initialise();

        // Initialise boundary data
        MapBoundaryData.Create(boundaryRegion.transform);

        // Initialise block animator
        BlockAnimator.MovingCurveAsset = blockMovementCurve;
        BlockAnimator.LandingCurveAsset = blockLandCurve;
    }
}
