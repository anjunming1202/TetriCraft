using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class SingleGameController : GameController
{
    [SerializeField] PlayerGameManager gameManager;
    [SerializeField] PlayerInput controllerPlayerInput;

    public override PlayerGameManager GetGameManager(PlayerID playerID = PlayerID.P1)
    {
        return gameManager;
    }

    public override MapBoundaryData GetBoundaryData(PlayerID playerID = PlayerID.P1)
    {
        return gameManager.boundaryData;
    }

    public override PlayerInput GetPlayerInput(PlayerID playerID = PlayerID.P1)
    {
        return controllerPlayerInput;
    }

    protected override void Initialise()
    {
        Debug.Log("Now Initialise Resources!");

        // init static resources
        gameManager.Initialise();

        // subscribe events
        gameManager.OnPlayerBoardDead += _ => GameOver();

        matchStateMachine.Initialise();
    }

    protected override async UniTask NewGame()
    {
        Debug.Log("Now New Game!");

        await PrepareNewGame();
        await IntroGame();
        StartGame();
    }

    protected override async UniTask PrepareNewGame()
    {
        Debug.Log("Now Game Init!");

        await gameManager.PrepareNewPlayerGame();
        matchStateMachine.PreparePreGame();
    }

    protected override async UniTask IntroGame()
    {
        Debug.Log("Now Game Intro!");

        await gameManager.PlayIntro();
        matchStateMachine.Intro();
    }

    protected override void StartGame()
    {
        Debug.Log("Now Game Start!");

        // start time flow
        SetGlobalTimePaused(false);

        // start game tick
        TickManager.Init();

        // Start player game
        gameManager.StartGameplay();
        matchStateMachine.StartGame();
    }

    protected override void PlayingUpdate()
    {
        gameManager.UpdateGameplay();
    }

    public override void PauseGame()
    {
        Debug.Log("Now Pause Game!");

        SetGlobalTimePaused(true);
        gameManager.PauseGameplay();
        matchStateMachine.Pause();
    }

    public override void ResumeGame()
    {
        Debug.Log("Now Resume Game!");

        SetGlobalTimePaused(false);
        gameManager.ResumeGameplay();
        matchStateMachine.Resume();
    }

    protected override void GameOver()
    {
        Debug.Log("Now Game Over!");

        SetGlobalTimePaused(true);
        gameManager.GameOver();
        matchStateMachine.GameOver();
    }

    protected override void CleanUpMatch()
    {
        Debug.Log("Now Game Clean Up!");

        SetGlobalTimePaused(false);
        gameManager.CleanUpBoard();
        matchStateMachine.CleanUp();
    }

    protected override void Dispose()
    {
        gameManager.OnPlayerBoardDead -= _ => GameOver();
    }
}
