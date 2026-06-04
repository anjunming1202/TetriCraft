using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameStateMachineAsync : StaticInstance<GameStateMachineAsync>
{
    public static GameStateType State => _currentState.Type;
    public static event Action<GameState, GameState> OnStateChange;

    private static GameState _currentState = _states[typeof(InitState)];
    private static Dictionary<Type, GameState> _states = new Dictionary<Type, GameState>()
    {
        { typeof(InitState), new InitState() },
        { typeof(IntroState), new IntroState() },
        { typeof(PlayingState) , new PlayingState() },
        { typeof(PausedState), new PausedState() },
        { typeof(GameOverState), new GameOverState() },
    };

    public async UniTask Init()
    {
        await ChangeStateAsync<InitState>();
        GameEvents.OnInitialised?.Invoke();

        Debug.Log("Game state: Initialised");
    }

    public async UniTask Intro()
    {
        if (State == GameStateType.Initialised)
        {
            GameEvents.OnIntroStart?.Invoke();
            await ChangeStateAsync<IntroState>();
            Debug.Log("Game state: Intro");
        }
        else
            Debug.LogError($"Invalid state transition of Countdown, trying to transit from {State} to {GameStateType.Intro}");
    }

    public async UniTask StartGame()
    {
        if (State == GameStateType.Intro)
        {
            await ChangeStateAsync<PlayingState>();
            GameEvents.OnGameStart?.Invoke();
            Debug.Log("Game state: Playing");
        }
        else
            Debug.LogError($"Invalid state transition of Start, trying to transit from {State} to {GameStateType.Playing}");
    }

    public async UniTask Pause()
    {
        if (State == GameStateType.Playing)
        {
            await ChangeStateAsync<PausedState>();
            GameEvents.OnGamePaused?.Invoke();
            Debug.Log("Game state: Paused");
        }
        else
        {
            Debug.LogError($"Invalid state transition of Pause, trying to transit from {State} to {GameStateType.Paused}");
        }
    }

    public async UniTask Resume()
    {
        if (State == GameStateType.Paused)
        {
            await ChangeStateAsync<PlayingState>();
            GameEvents.OnGameResumed?.Invoke();
            Debug.Log("Game state: Playing");
        }
        else
            Debug.LogError($"Invalid state transition of Resume, trying to transit from {State} to {GameStateType.Playing}");
    }

    public async UniTask GameOver()
    {
        if (State == GameStateType.Playing)
        {
            await ChangeStateAsync<GameOverState>();
            GameEvents.OnGameOver?.Invoke();
            Debug.Log("Game state: GameOver");
        }
        else
            Debug.LogError($"Invalid state transition of GameOver, trying to transit from {State} to {GameStateType.GameOver}");
    }

    private async UniTask ChangeStateAsync<T>() where T : GameState
    {
        if (_currentState != null)
            await _currentState.Exit();

        var prevState = _currentState;
        var nextState = _states[typeof(T)];
        _currentState = nextState;

        OnStateChange?.Invoke(prevState, nextState);

        await _currentState.Enter();
    }
}
