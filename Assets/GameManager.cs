using UnityEngine;

// Game Manager for managing the whole game
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Map")]
    // Map Manager
    public MapManager mapManager; // inspector

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
    public MapDebugger mapDebugger; // inspector

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Load static resources
        InitialiseResources();

        // Debugger
        mapDebugger.DebugMap(mapManager.map);
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
