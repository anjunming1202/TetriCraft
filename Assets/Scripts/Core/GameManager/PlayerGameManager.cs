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

    [Header("Visual")]
    public AnimationCurveAsset blockMovementCurve;
    public AnimationCurveAsset blockLandCurve;

    [Header("Controllers")]
    [SerializeField] public IntroController introController;
    [SerializeField] private NextTetrominoUIController nextTetrominoUIController;

    // Events
    public event Action<PlayerID> OnPlayerBoardDead;

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
        nextTetrominoUIController.SetTetrisManager(tetrisManager);
        nextTetrominoUIController.InitialiseAsync().Forget();

        // Initialise boundary data
        boundaryData = MapBoundaryData.Create(boundaryRegion.transform);

        // Initialise block animator
        BlockAnimator.MovingCurveAsset = blockMovementCurve;
        BlockAnimator.FastMovingCurveAsset = blockLandCurve;
    }

    public UniTask PrepareNewPlayerGame()
    {
        // Initialise map
        tetrisManager.PrepareNewTetrisMap(boundaryData.width, boundaryData.height, playerID);

        // Initialise scorer
        scoreManager.Reset();

        return UniTask.CompletedTask;
    }

    public async UniTask PlayIntro()
    {
        await introController.PlayAsync();
    }

    public void StartGameplay()
    {
        tetrisManager.StartNewMap();
    }

    public void UpdateGameplay()
    {
        tetrisManager.OnUpdate();
    }

    public void PauseGameplay()
    {
        tetrisManager.StopUpdating();
    }

    public void ResumeGameplay()
    {
        tetrisManager.ResumeUpdating();
    }

    public void GameOver()
    {
        tetrisManager.StopUpdating();
    }

    public void CleanUpBoard()
    {
        tetrisManager.CleanUpTetrisMap();

        nextTetrominoUIController.ClearTetrominoIcons();
    }

    public void NotifyBoardGameDead()
    {
        OnPlayerBoardDead?.Invoke(playerID);
    }
}
