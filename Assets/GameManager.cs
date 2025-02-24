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

    // Boundary Region Definer
    public Transform boundaryRegion;

    // Game Objects Container
    private List<Transform> blockObjects;


    void Awake()
    {
        // Load resources
        LoadStaticResources();

        // Initialise map
        mapManager = FindObjectOfType<MapManager>();
        mapManager.NewMap();

        // Initialise block object list
        blockObjects = new List<Transform>();
    }
    void Start()
    {

        // Test
        SpawnNewTetromino();
    }

    ////////////////////////////////////////////////////
    void Update()
    {
        
    }

    void OnRenderObject()
    {

    }



    ////////////////////////////////////////////////////

    public void SpawnNewTetromino()
    {
        // New tetromino data
        mapManager.SpawnTetromino();

        // Instantiate the tetromino and keep block objects reference
        InstantiateTetromino(mapManager.CurrentTetromino);
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
    private void LoadStaticResources()
    {
        // Load block resources
        BlockResources.LoadBlockPrefabs();
        BlockResources.LoadBlockTextures();

        // Initialise boundary data
        MapBoundaryData.Create(boundaryRegion);
    }
}
