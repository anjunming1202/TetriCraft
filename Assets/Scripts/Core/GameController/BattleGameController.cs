using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleGameController : GameController
{
    [SerializeField] PlayerGameManager gameManagerP1;
    [SerializeField] PlayerGameManager gameManagerP2;

    [SerializeField] PlayerInput controllerPlayerInputP1;
    [SerializeField] PlayerInput controllerPlayerInputP2;

    private PlayerID? _winner;

    public override PlayerGameManager GetGameManager(PlayerID playerID)
    {
        return playerID == PlayerID.P1 ? gameManagerP1 : gameManagerP2;
    }

    public override MapBoundaryData GetBoundaryData(PlayerID playerID)
    {
        return playerID == PlayerID.P1 ? gameManagerP1.boundaryData : gameManagerP2.boundaryData;
    }

    public override PlayerInput GetPlayerInput(PlayerID playerID = PlayerID.P1)
    {
        return playerID == PlayerID.P1 ? controllerPlayerInputP1 : controllerPlayerInputP2;
    }

    protected override void Initialise()
    {
        Debug.Log("Now Initialise Resources!");

        gameManagerP1.Initialise();
        gameManagerP2.Initialise();

        gameManagerP1.OnPlayerBoardDead += HandlePlayerDead;
        gameManagerP2.OnPlayerBoardDead += HandlePlayerDead;

        matchStateMachine.Initialise();
    }

    protected override async UniTask NewGame()
    {
        Debug.Log("Now New Game!");

        _winner = null;
        await PrepareNewGame();
        await IntroGame();
        StartGame();
    }

    protected override async UniTask PrepareNewGame()
    {
        Debug.Log("Now Game Init!");

        await UniTask.WhenAll(
            gameManagerP1.PrepareNewPlayerGame(),
            gameManagerP2.PrepareNewPlayerGame()
        );
        matchStateMachine.PreparePreGame();
    }

    protected override async UniTask IntroGame()
    {
        Debug.Log("Now Game Intro!");

        await UniTask.WhenAll(
            gameManagerP1.PlayIntro(),
            gameManagerP2.PlayIntro()
        );
        matchStateMachine.Intro();
    }

    protected override void StartGame()
    {
        Debug.Log("Now Game Start!");

        SetGlobalTimePaused(false);
        TickManager.Init();
        gameManagerP1.StartGameplay();
        gameManagerP2.StartGameplay();
        matchStateMachine.StartGame();
    }

    protected override void PlayingUpdate()
    {
        gameManagerP1.UpdateGameplay();
        gameManagerP2.UpdateGameplay();
    }

    public override void PauseGame()
    {
        Debug.Log("Now Pause Game!");

        SetGlobalTimePaused(true);
        gameManagerP1.PauseGameplay();
        gameManagerP2.PauseGameplay();
        matchStateMachine.Pause();
    }

    public override void ResumeGame()
    {
        Debug.Log("Now Resume Game!");

        SetGlobalTimePaused(false);
        gameManagerP1.ResumeGameplay();
        gameManagerP2.ResumeGameplay();
        matchStateMachine.Resume();
    }

    private void HandlePlayerDead(PlayerID loserID)
    {
        if (GameStateMachine.State != GameStateType.Playing) return;
        _winner = loserID == PlayerID.P1 ? PlayerID.P2 : PlayerID.P1;
        GameOver();
    }

    protected override void GameOver()
    {
        Debug.Log($"[Battle] Game Over! Winner: {_winner?.ToString() ?? "Draw"}");

        SetGlobalTimePaused(true);
        gameManagerP1.GameOver();
        gameManagerP2.GameOver();
        matchStateMachine.GameOver();
    }

    protected override void CleanUpMatch()
    {
        Debug.Log("Now Game Clean Up!");

        _winner = null;
        SetGlobalTimePaused(false);
        gameManagerP1.CleanUpBoard();
        gameManagerP2.CleanUpBoard();
        matchStateMachine.CleanUp();
    }

    protected override void Dispose()
    {
        gameManagerP1.OnPlayerBoardDead -= HandlePlayerDead;
        gameManagerP2.OnPlayerBoardDead -= HandlePlayerDead;
    }
}
