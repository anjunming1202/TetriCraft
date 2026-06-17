using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleGameController : GameController
{
    [SerializeField] PlayerGameManager gameManagerP1;
    [SerializeField] PlayerGameManager gameManagerP2;

    [SerializeField] PlayerInput controllerPlayerInputP1;
    [SerializeField] PlayerInput controllerPlayerInputP2;

    [SerializeField] private GarbageConfig garbageConfig;

    private PlayerID? _winner;

    private BattleTetrisManager BattleTetrisP1 => (BattleTetrisManager)gameManagerP1.tetrisManager;
    private BattleTetrisManager BattleTetrisP2 => (BattleTetrisManager)gameManagerP2.tetrisManager;

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
        Debug.Log("[BattleGameController] Initialise — P1 tetrisManager type: " + gameManagerP1.tetrisManager?.GetType().Name);
        Debug.Log("[BattleGameController] Initialise — P2 tetrisManager type: " + gameManagerP2.tetrisManager?.GetType().Name);

        gameManagerP1.Initialise();
        gameManagerP2.Initialise();

        BattleTetrisP1.OnTurnLineClearsComplete += HandleCycleCompleteP1;
        BattleTetrisP2.OnTurnLineClearsComplete += HandleCycleCompleteP2;
        Debug.Log("[BattleGameController] Subscribed to OnCycleComplete for both players");

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

        BattleTetrisP1.SetGarbageConfig(garbageConfig);
        BattleTetrisP2.SetGarbageConfig(garbageConfig);

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

        gameManagerP1.GameOver();
        gameManagerP2.GameOver();
        GameOverAsync().Forget();
    }

    private async UniTask GameOverAsync()
    {
        await UniTask.WaitUntil(
            () => !BattleTetrisP1.AnyBlockAnimating() && !BattleTetrisP2.AnyBlockAnimating(),
            cancellationToken: this.GetCancellationTokenOnDestroy()
        );
        SetGlobalTimePaused(true);
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
        BattleTetrisP1.OnTurnLineClearsComplete -= HandleCycleCompleteP1;
        BattleTetrisP2.OnTurnLineClearsComplete -= HandleCycleCompleteP2;
    }

    private void HandleCycleCompleteP1(uint cycleClears, uint cycleCombo)
        => SendGarbage(BattleTetrisP2, cycleClears, cycleCombo);

    private void HandleCycleCompleteP2(uint cycleClears, uint cycleCombo)
        => SendGarbage(BattleTetrisP1, cycleClears, cycleCombo);

    private void SendGarbage(BattleTetrisManager opponent, uint cycleClears, uint cycleCombo)
    {
        if (garbageConfig == null) return;
        int garbage = garbageConfig.CalculateGarbage(cycleClears, cycleCombo);
        Debug.Log($"[BattleGameController] Cycle clears={cycleClears}, combo={cycleCombo} → sending {garbage} garbage to {opponent.PlayerID}");
        if (garbage > 0)
            opponent.QueueGarbage(garbage);
    }
}
