using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// Game Manager for managing the whole game
public class GameManager : MonoBehaviour
{
    // Map Manager
    private MapManager mapManager;

    // Map region
    public SpriteMask boundaryRegion;

    // Map Data (readonly)
    private Map map => mapManager.map;
    private int width => MapBoundaryData.Instance.width;
    private int height => MapBoundaryData.Instance.height;
    private MapBoundaryData boundary => MapBoundaryData.Instance;

    // Timer
    private float timer = 0;

    // Control
    //private bool isAccelerating = false;
    public float speedFalling = 1;
    public float speedAcclerating = 2;
    private float interval;
    private float intervalNormal => 1 / speedFalling;
    private float intervalAccelerating => 1 / speedAcclerating;

    // Game Objects Container
    private List<Transform> blockObjects;

    // Input Key Mapping
    private KeyCode key_left => KeyCode.A;
    private KeyCode key_right => KeyCode.D;
    private KeyCode key_accelerate => KeyCode.S;
    private KeyCode key_land => KeyCode.Space;
    private KeyCode key_rotateCW => KeyCode.E;
    private KeyCode key_rotateCCW => KeyCode.Q;


    void Awake()
    {
        // Load resources
        LoadStaticResources();
    }
    public void NewGame()
    {
        // Initialise map
        mapManager = FindObjectOfType<MapManager>();
        mapManager.NewMap();

        // Initialise block object list
        if (blockObjects != null)
        {
            foreach (Transform t in blockObjects)
                GameObject.Destroy(t);
        }
        blockObjects = new List<Transform>();

        // Spawn the first tetromino
        SpawnTetromino();

        // Initialise falling speed
        interval = intervalNormal;
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
        foreach (var t in blockObjects)
        {
            if (t.GetComponent<BlockRenderer>().block.isFalling == false)
            {
                landedCount++;
            }
        }
        int blockCount = 0;
        foreach (var t in mapManager.map.blockMap)
        {
            if (t != null)
            {
                blockCount++;
            }
        }
        Debug.Log($"blocks in map: {blockCount}, blocks instantiated: {landedCount}");

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
            OnAccelerating();
        }
        if (Input.GetKeyUp(key_accelerate))
        {
            StopAccelerating();
        }
        if (Input.GetKeyDown(key_land)) // Land
        {
            mapManager.Land();
        }
        if (Input.GetKeyDown(key_rotateCW)) // Rotate clockwise
        {
            mapManager.Rotate(true);
        }
        if (Input.GetKeyDown(key_rotateCCW)) // Rotate anticlockwise
        {
            mapManager.Rotate(false);
        }

        // Fall of tetromino
        if (timer >= interval)
        {
            mapManager.Fall();
            timer = 0;
        }

        // Spawn new tetromino by checking the falling tetromino state (is isLanded)
        TrySpawnTetromino();
    }

    public void OnAccelerating()
    {
        interval = intervalAccelerating;
    }
    private void StopAccelerating()
    {
        interval = intervalNormal;
    }

    ////////////////////////////////////////////////////

    public void TrySpawnTetromino()
    {
        if (mapManager.CurrentTetromino.isLanded)
        {
            // Set blocks children to the block pool
            TetrominoFactory.ReparentBlocks();

            // Spawn new tetromino    
            SpawnTetromino();
        }
    }
    public void SpawnTetromino()
    {
        // New tetromino data
        mapManager.SpawnTetromino();

        // Instantiate the tetromino and keep block objects reference
        InstantiateTetromino(mapManager.CurrentTetromino);
    }
    private void InstantiateTetromino(Tetromino tetromino)
    {
        GameObject tetrominoObject = TetrominoFactory.CreateTetromino(tetromino);
        foreach (Transform block in tetrominoObject.GetComponentInChildren<Transform>())
        {
            blockObjects.Add(block);
        }
    }

    // Initialising Helper Functions
    private void LoadStaticResources()
    {
        // Load block resources
        BlockResources.LoadBlockPrefabs();
        BlockResources.LoadBlockTextures();

        // Initialise boundary data
        MapBoundaryData.Create(boundaryRegion.transform);
    }
}
