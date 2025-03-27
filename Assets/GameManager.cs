using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

// Game Manager for managing the whole game
public class GameManager : MonoBehaviour
{
    [Header("Map")]
    // Map Manager
    public MapManager mapManager; // inspector

    // Map Region
    public SpriteMask boundaryRegion; // inspector

    [Header("Score")]
    public ScoreManager scoreManager; // inspector

    [Header("Game State")]
    [SerializeField] private static bool gameover = false; // Game over flag

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

        // Initialise game state
        gameover = false;

        // Initialise scorer
        scoreManager.LinkToGame(mapManager);
        scoreManager.Reset();
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
        mapManager.UpdatingOver();
        Debug.Log("Now Game Over!");
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
