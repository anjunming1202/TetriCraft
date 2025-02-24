using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// Game Manager for managing the whole game
public class GameManager : MonoBehaviour
{
    // Grid
    private Map grid;
    private int width => MapBoundaryData.Instance.width;
    private int height => MapBoundaryData.Instance.height;
    // Boundary
    public Transform boundaryRegion;
    public MapBoundaryData boundary; // static resource

    // Falling Tetromino
    [SerializeField] private Tetromino fallingTetromino;


    // Game objects
    private List<Transform> blockObjects;


    void Awake()
    {
        // Load resources
        LoadResources();

        // Initialise grid
        grid = new Map();

        // Initialise block object list
        blockObjects = new List<Transform>();
    }
    void Start()
    {

        // Test
        SpawnTetromino(TetrominoType.T);
    }

    ////////////////////////////////////////////////////
    void Update()
    {
        
    }

    void OnRenderObject()
    {

    }



    ////////////////////////////////////////////////////

    public void SpawnTetromino(TetrominoType type)
    {
        // New a tetromino
        fallingTetromino = new Tetromino(type);

        // Set tetromino position (at the centre)
        int x = (width - fallingTetromino.size) / 2;
        fallingTetromino.position = new Vector2Int(x, height);

        // Rotate tetromino randomly

        // Place tetromino to spawn point
        grid.AddTetromino(fallingTetromino);

        // Store reference of block objects to a container
        InstantiateTetromino(fallingTetromino);
    }
    private void InstantiateTetromino(Tetromino tetromino)
    {
        foreach (Transform block in TetrominoFactory.CreateTetromino(tetromino).GetComponentInChildren<Transform>())
        {
            blockObjects.Add(block);
        }
    }

    public void Fall()
    {

    }

    public void Rotate(bool isclockwise = true)
    {

    }

    public void AccelerateFall()
    {

    }

    public void Land()
    {

    }

    // Initialising Helper Functions
    private void LoadResources()
    {
        // Load block resources
        BlockResources.LoadBlockPrefabs();
        BlockResources.LoadBlockTextures();

        // Initialise boundary data
        boundary = MapBoundaryData.Create(boundaryRegion);
    }
}
