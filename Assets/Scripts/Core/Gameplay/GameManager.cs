using System;
using UnityEngine;

// Game Manager for managing the whole game
public class GameManager : Singleton<GameManager>
{
    [Header("Map")]
    // Map Manager
    public TetrisManager tetrisManager; // inspector

    // Map Region
    public SpriteMask boundaryRegion; // inspector

    [Header("Score")]
    public ScoreManager scoreManager; // inspector

    [Header("Game State")]
    public bool gameover = false; // Game over flag

    [Header("Visual")]
    public AnimationCurveAsset blockMovementCurve;
    public AnimationCurveAsset blockLandCurve;

    [Header("Debug")]
    public bool debug = true;

    public event Action OnPause;
    public event Action OnResume;
    public bool IsPaused { get; private set; } = false;

    protected override void Awake()
    {
        base.Awake();

        // Load static resources
        InitialiseResources();
    }
    public void NewGame()
    {
        // Initialise map
        tetrisManager.NewMap(MapBoundaryData.Instance.width, MapBoundaryData.Instance.height);

        // Initialise game state
        gameover = false;

        // Initialise scorer
        scoreManager.LinkToGame(tetrisManager);
        scoreManager.Reset();
    }
    public void StartGame()
    {
        tetrisManager.StartNewMap();
        IsPaused = false;
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

    void Start()
    {
        // New game
        NewGame();
        StartGame();
    }

    ////////////////////////////////////////////////////
    void Update()
    {
        if (!IsPaused)
        {
            tetrisManager.OnUpdate();

            if (!gameover)
            {
                if (tetrisManager.CheckGameover())
                    OnGameover();
            }
            else
                Debug.Log("Game Over");
        }
    }

    private void OnGameover()
    {
        gameover = true;
        tetrisManager.StopUpdating();
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
        BlockAnimator.FastMovingCurveAsset = blockLandCurve;
    }
}
