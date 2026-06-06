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
    [SerializeField] private NextTetrominoUIController nextTetrominoUIController;

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

        // Initialise tetris manager
        tetrisManager.Initialise();

        // Initialise scorer manager
        scoreManager.SubscribeMap(tetrisManager);

        // Initialise ui controllers
        nextTetrominoUIController.InitialiseAsync().Forget();

        // Initialise boundary data
        boundaryData = MapBoundaryData.Create(boundaryRegion.transform);

        // Initialise block animator
        BlockAnimator.MovingCurveAsset = blockMovementCurve;
        BlockAnimator.FastMovingCurveAsset = blockLandCurve;

        gameStateMachine.Initialise();
    }

    public UniTask PrepareNewPlayerGame()
    {
        // Initialise map
        tetrisManager.PrepareNewTetrisMap(boundaryData.width, boundaryData.height, playerID);

        // Initialise scorer
        scoreManager.Reset();

        // Initialise game state
        gameStateMachine.PreparePreGame();

        return UniTask.CompletedTask;
    }

    public async UniTask PlayIntro()
    {
        await introController.PlayAsync();

        gameStateMachine.Intro();
    }

    public void StartGameplay()
    {
        tetrisManager.StartNewMap();

        gameStateMachine.StartGame();
    }

    public void UpdateGameplay()
    {
        tetrisManager.OnUpdate();
    }

    public void PauseGameplay()
    {
        tetrisManager.StopUpdating();

        // debug
        debug = false;

        gameStateMachine.Pause();
    }

    public void ResumeGameplay()
    {
        tetrisManager.ResumeUpdating();

        // debug
        debug = true;

        gameStateMachine.Resume();
    }

    public void GameOver()
    {
        tetrisManager.StopUpdating();

        gameStateMachine.GameOver();
    }

    public void CleanUpBoard()
    {
        tetrisManager.CleanUpTetrisMap();

        nextTetrominoUIController.ClearTetrominoIcons();

        gameStateMachine.CleanUp();
    }

    public void NotifyBoardGameDead()
    {
        OnPlayerBoardDead?.Invoke(playerID);
    }
}
