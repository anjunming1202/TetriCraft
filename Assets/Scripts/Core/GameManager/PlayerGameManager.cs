using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Game Manager for managing the whole game
[RequireComponent(typeof(GameInputController))]
public class PlayerGameManager : MonoBehaviour
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

    // Events
    public event Action<PlayerID> OnPlayerBoardDead;


    [Header("Debug")]
    public bool debug = true;
    public bool IsPaused => GameStateMachine.State == GameStateType.Paused;



    /// <summary>
    /// Initialising all static data/resources
    /// </summary>
    public void Initialise()
    {
        tetrisManager.OnGameDead += NotifyBoardGameDead;

        // Initialise boundary data
        boundaryData = MapBoundaryData.Create(boundaryRegion.transform);

        // Initialise block animator
        BlockAnimator.MovingCurveAsset = blockMovementCurve;
        BlockAnimator.FastMovingCurveAsset = blockLandCurve;
    }

    public void UpdatePlaying()
    {
        tetrisManager.OnUpdate();
    }

    public void PauseGame()
    {
        tetrisManager.StopUpdating();

        gameStateMachine.Pause();

        // debug
        debug = false;
    }

    public void ResumeGame()
    {
        tetrisManager.ResumeUpdating();

        gameStateMachine.Resume();

        // debug
        debug = true;
    }

    ////////////////////////////////////////////////////
    public UniTask InitNewGame()
    {
        // Initialise map
        tetrisManager.InitMap(boundaryData.width, boundaryData.height, playerID);

        // Initialise game state
        gameStateMachine.Init();

        // Initialise scorer
        scoreManager.LinkToGame(tetrisManager);
        scoreManager.Reset();

        return UniTask.CompletedTask;
    }

    public async UniTask IntroGame()
    {
        await introController.ActAsync();

        gameStateMachine.Intro();
    }

    public void StartGame()
    {
        tetrisManager.StartNewMap();

        gameStateMachine.StartGame();
    }

    public void GameOver()
    {
        tetrisManager.StopUpdating();

        gameStateMachine.GameOver();
    }

    public void CleanUp()
    {

    }

    public void NotifyBoardGameDead()
    {
        OnPlayerBoardDead?.Invoke(playerID);
    }
}
