using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// Game Manager for managing the whole game
public class GameManager : MonoBehaviour
{
    // Map Manager
    private MapManager mapManager;

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

    // Boundary Region Definer
    public Transform boundaryRegion;

    // Game Objects Container
    private List<Transform> blockObjects;

    // Input Key Mapping
    private KeyCode MoveLeftKey => KeyCode.A;
    private KeyCode MoveRightKey => KeyCode.D;
    private KeyCode FallDownKey => KeyCode.S;
    private KeyCode LandKey => KeyCode.Space;


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
        // Timer
        timer += Time.deltaTime;

        // Control
        if (Input.GetMouseButtonDown(0))
        {
            // Test
            //SpawnTetromino();
        }
        if (Input.GetKeyDown(MoveLeftKey)) // Left
        {
            mapManager.Left();
        }
        if (Input.GetKeyDown(MoveRightKey)) // Right
        {
            mapManager.Right();
        }
        if (Input.GetKeyDown(FallDownKey)) // Accelerating
        {
            OnAccelerating();
        }
        if (Input.GetKeyUp(FallDownKey))
        {
            StopAccelerating();
        }
        if (Input.GetKeyDown(LandKey)) // Land
        {
            mapManager.Land();
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
        MapBoundaryData.Create(boundaryRegion);
    }
}
