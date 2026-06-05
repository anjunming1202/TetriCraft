using Cysharp.Threading.Tasks;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PauseManager))]
public abstract class GameController : Singleton<GameController>
{
    //game state machine
    //binding entry functions at game states, as the global only entrance

    public abstract PlayerGameManager GetGameManager(PlayerID playerID = PlayerID.P1);

    public abstract MapBoundaryData GetBoundaryData(PlayerID playerID = PlayerID.P1);

    public abstract PlayerInput GetPlayerInput(PlayerID playerID = PlayerID.P1);

    public bool IsPaused => GameStateMachine.State == GameStateType.Paused;

    protected override void Awake()
    {
        base.Awake();

        // Global only entry of awake initialisation
        Initialise(); 
    }

    protected void Start()
    {
        InputRoot.DisableOutOfGameUIInput();

        // Global only entry of entering new game process
        NewGame().Forget(); 
    }

    protected void Update()
    {
        switch (GameStateMachine.State)
        {
            case GameStateType.Playing:
                // Global only entry of playing state update
                PlayingUpdate();
                break;
            
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        InputRoot.EnableOutOfGameUIInput();

        CleanUp();
    }

    /// <summary>
    /// Initialisation when the global controller awake 
    /// </summary>
    protected abstract void Initialise();

    /// <summary>
    /// Entry point of new game workflow
    /// </summary>
    protected abstract UniTask NewGame();

    /// <summary>
    /// Init the new game, game state becomes initialised
    /// </summary>
    protected abstract UniTask InitNewGame();

    /// <summary>
    /// Intro play of the new game, game state at intro
    /// </summary>
    protected abstract UniTask IntroGame();

    /// <summary>
    /// Start the new game, game state becomes playing
    /// </summary>
    protected abstract void StartGame();

    /// <summary>
    /// Entry point of the game update when playing
    /// </summary>
    protected abstract void PlayingUpdate();

    /// <summary>
    /// Pause the game
    /// </summary>
    public abstract void PauseGame();

    /// <summary>
    /// Resume the game
    /// </summary>
    public abstract void ResumeGame();

    /// <summary>
    /// Announce whole-game-level game over, execute game over logic, game state becomes playing
    /// </summary>
    protected abstract void GameOver();


    /// <summary>
    /// After gameover, before quit or new game, clean up the scene
    /// </summary>
    protected abstract void CleanUp();



    protected void SetGlobalTimePaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
    }
}