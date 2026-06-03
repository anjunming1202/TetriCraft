using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Game Manager for managing the whole game
[RequireComponent(typeof(GameInputController))]
public class GameManager : MonoBehaviour
{
    [SerializeField] public PlayerID playerID;

    [Header("Map")]
    // Map Manager
    public TetrisManager tetrisManager; // inspector

    // Map Region
    public SpriteMask boundaryRegion; // inspector
    public MapBoundaryData boundaryData { get; private set; }

    [Header("Score")]
    public ScoreManager scoreManager; // inspector

    [Header("Game State")]
    public bool gameover = false; // Game over flag
    public event Action OnGameStart;
    public event Action OnGameOver;
    public event Action OnPause;
    public event Action OnResume;

    [Header("Visual")]
    public AnimationCurveAsset blockMovementCurve;
    public AnimationCurveAsset blockLandCurve;


    [Header("Debug")]
    public bool debug = true;
    public bool IsPaused { get; private set; } = false;

    private void Awake()
    {
        // Load static resources
        InitialiseResources();
    }

    void Start()
    {
        // New game
        NewGame();
    }

    private void Update()
    {
        if (!IsPaused)
        {
            tetrisManager.OnUpdate();
        }
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    public void NewGame()
    {
        // Init
        InitNewGame();
        // Start
        StartGame();
    }

    public void PauseGame()
    {
        IsPaused = true;
        tetrisManager.StopUpdating();
        Time.timeScale = 0f;
        OnPause?.Invoke();
        // debug
        debug = false;
    }

    public void ResumeGame()
    {
        IsPaused = false;
        tetrisManager.ResumeUpdating();
        Time.timeScale = 1f;
        OnResume?.Invoke();
        // debug
        debug = true;
    }

    ////////////////////////////////////////////////////
    private void InitNewGame()
    {
        // Initialise map
        tetrisManager.InitMap(boundaryData.width, boundaryData.height, this);

        // Initialise game state
        tetrisManager.OnGameOver += GameOver;
        gameover = false;

        // Initialise scorer
        scoreManager.LinkToGame(tetrisManager);
        scoreManager.Reset();
    }

    private void StartGame()
    {
        tetrisManager.StartNewMap();
        IsPaused = false;
        Time.timeScale = 1f;

        OnGameStart?.Invoke();
    }

    private void GameOver()
    {
        Debug.Log("Now Game Over!");

        gameover = true;
        tetrisManager.StopUpdating();
        Time.timeScale = 0f;

        OnGameOver?.Invoke();
    }

    private void CleanUp()
    {
        Time.timeScale = 1f;
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
        boundaryData = MapBoundaryData.Create(boundaryRegion.transform);

        // Initialise block animator
        BlockAnimator.MovingCurveAsset = blockMovementCurve;
        BlockAnimator.FastMovingCurveAsset = blockLandCurve;
    }
}
