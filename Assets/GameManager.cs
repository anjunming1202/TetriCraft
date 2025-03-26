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

    // Map Data (readonly)
    private int width => MapBoundaryData.Instance.width;
    private int height => MapBoundaryData.Instance.height;
    private MapBoundaryData boundary => MapBoundaryData.Instance;

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
        mapManager.NewMap();

        mapManager.OnLineClear += ScoreLineClear;
        mapManager.OnFinishTurn += OnNextTurn;

        mapManager.fallingTetromino.OnTetrominoSoftDrop += ScoreSoftDrop;
        mapManager.fallingTetromino.OnTetrominoHardDrop += ScoreHardDrop;

        // Initialise game logic
        gameover = false;
        score = 0;

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



    private void ScoreSoftDrop(MapTetromino tetromino)
    {
        score += 1;
    }
    private void ScoreHardDrop(MapTetromino tetromino)
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
