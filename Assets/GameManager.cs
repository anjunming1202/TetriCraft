using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

// Game Manager for managing the whole game
public class GameManager : MonoBehaviour
{
    // Map Manager
    public MapManager mapManager; // inspector

    // Tetrominos
    public DummyTetromino nextTetromino; // inspector

    [Header("Map")]
    // Map Region
    public SpriteMask boundaryRegion; // inspector

    [Header("Score")]
    public ScoreManager scoreManager; // inspector

    [Header("Game State")]
    [SerializeField, ReadOnly] private bool gameover = false; // Game over flag

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
        mapManager.NewMap(MapBoundaryData.Instance.width, MapBoundaryData.Instance.height);

        mapManager.OnFinishTurn += OnNextTurn;

        // Initialise game state
        gameover = false;

        // Initialise scorer
        scoreManager.LinkToGame(mapManager);
        scoreManager.Reset();

        // Spawn the first tetromino
        TetrominoGenerator.NewRandomTetromino(nextTetromino);
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
        landedCount = mapManager.GetComponentsInChildren<Block>().Length;      
        Debug.Log($"blocks in map: {mapManager.blockCount}, blocks instantiated: {landedCount}");

        if (!gameover)
        {
            if (mapManager.CheckGameover())
                Gameover();
        }
        else
            Debug.Log("Game Over");
    }

    private void Gameover()
    {
        gameover = true;
        Debug.Log("Now Game Over!");
    }

    void UpdateGame()
    {

    }


    //================================//
    // Map Control Logic
    //================================//

    private void OnNextTurn()
    {
        // Stop when game over
        if (gameover)
            return;

        // Spawn new tetromino in map
        mapManager.SpawnTetromino(nextTetromino);

        // Create next new tetromino
        TetrominoGenerator.NewRandomTetromino(nextTetromino);

        // Display next tetromino
        nextTetromino.Display();
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
