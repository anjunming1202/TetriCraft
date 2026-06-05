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

        // supscribe events
        gameManager.OnPlayerBoardDead += (PlayerID) => GameOver();
    }

    protected override async UniTask NewGame()
    {
        Debug.Log("Now New Game!");

        await InitNewGame();
        await IntroGame();
        StartGame();
    }

    protected override async UniTask InitNewGame()
    {
        Debug.Log("Now Game Init!");

        await gameManager.InitNewGame();
    }

    protected override async UniTask IntroGame()
    {
        Debug.Log("Now Game Intro!");

        await gameManager.IntroGame();
    }

    protected override void StartGame()
    {
        Debug.Log("Now Game Start!");

        // start time flow
        SetGlobalTimePaused(false);

        // start game tick
        TickManager.Init();

        // Start player game
        gameManager.StartGame();
    }

    protected override void PlayingUpdate()
    {
        gameManager.UpdatePlaying();
    }

    public override void PauseGame()
    {
        Debug.Log("Now Pause Game!");

        SetGlobalTimePaused(true);
        gameManager.PauseGame();
    }

    public override void ResumeGame()
    {
        Debug.Log("Now Resume Game!");

        SetGlobalTimePaused(false);
        gameManager.ResumeGame();
    }

    protected override void GameOver()
    {
        Debug.Log("Now Game Over!");

        SetGlobalTimePaused(true);
        gameManager.GameOver();
    }

    protected override void CleanUp()
    {
        Debug.Log("Now Game Clean Up!");

        SetGlobalTimePaused(false);
        gameManager.CleanUp();
    }
}
