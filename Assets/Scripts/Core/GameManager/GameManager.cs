using Cysharp.Threading.Tasks;
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
    public GameStateMachine gameStateMachine;

    [Header("Visual")]
    public AnimationCurveAsset blockMovementCurve;
    public AnimationCurveAsset blockLandCurve;

    [Header("Controllers")]
    [SerializeField] public IntroController introController;


    [Header("Debug")]
    public bool debug = true;
    public bool IsPaused => GameStateMachine.State == GameStateType.Paused;

    private void Awake()
    {
        // Load static resources
        InitialiseResources();
    }

    void Start()
    {
        // New game
        _ = NewGame();
    }

    private void Update()
    {
        if (!IsPaused)
        {
            UpdatePlaying();
        }
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    public async UniTask NewGame()
    {
        // Init
        await InitNewGame();

        // Intro
        await IntroGame();

        // Start
        StartGame();
    }

    public void UpdatePlaying()
    {
        tetrisManager.OnUpdate();
    }

    public void PauseGame()
    {
        tetrisManager.StopUpdating();
        Time.timeScale = 0f;

        gameStateMachine.Pause();

        // debug
        debug = false;
    }

    public void ResumeGame()
    {
        tetrisManager.ResumeUpdating();
        Time.timeScale = 1f;

        gameStateMachine.Resume();

        // debug
        debug = true;
    }

    ////////////////////////////////////////////////////
    private UniTask InitNewGame()
    {
        // Initialise map
        tetrisManager.InitMap(boundaryData.width, boundaryData.height, playerID);

        // Initialise game state
        tetrisManager.OnGameDead += GameOver;
        gameStateMachine.Init();

        // Initialise scorer
        scoreManager.LinkToGame(tetrisManager);
        scoreManager.Reset();

        return UniTask.CompletedTask;
    }

    private async UniTask IntroGame()
    {
        await introController.ActAsync();

        gameStateMachine.Intro();
    }

    private void StartGame()
    {
        tetrisManager.StartNewMap();
        Time.timeScale = 1f;

        gameStateMachine.StartGame();
    }

    private void GameOver()
    {
        Debug.Log("Now Game Over!");

        tetrisManager.StopUpdating();
        Time.timeScale = 0f;

        gameStateMachine.GameOver();
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
