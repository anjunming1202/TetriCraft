using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameStateMachine : StaticInstance<GameStateMachine>
{
    public static GameStateType State => _currentState;
    public static event Action<GameStateType, GameStateType> OnStateChange;

    private static GameStateType _currentState = GameStateType.Loading;

    public void Init()
    {
        ChangeState(GameStateType.Initialised);
        GameEvents.OnInitialised?.Invoke();

        Debug.Log("Game state: Initialised");
    }

    public void Intro()
    {
        if (State == GameStateType.Initialised)
        {
            GameEvents.OnIntroStart?.Invoke();
            ChangeState(GameStateType.Intro);
            Debug.Log("Game state: Intro");
        }
        else
            Debug.LogError($"Invalid state transition of Countdown, trying to transit from {State} to {GameStateType.Intro}");
    }

    public void StartGame()
    {
        if (State == GameStateType.Intro)
        {
            ChangeState(GameStateType.Playing);
            GameEvents.OnGameStart?.Invoke();
            Debug.Log("Game state: Playing");
        }
        else
            Debug.LogError($"Invalid state transition of Start, trying to transit from {State} to {GameStateType.Playing}");
    }

    public void Pause()
    {
        if (State == GameStateType.Playing)
        {
            ChangeState(GameStateType.Paused);
            GameEvents.OnGamePaused?.Invoke();
            Debug.Log("Game state: Paused");
        }
        else
        {
            Debug.LogError($"Invalid state transition of Pause, trying to transit from {State} to {GameStateType.Paused}");
        }
    }

    public void Resume()
    {
        if (State == GameStateType.Paused)
        {
            ChangeState(GameStateType.Playing);
            GameEvents.OnGameResumed?.Invoke();
            Debug.Log("Game state: Playing");
        }
        else
            Debug.LogError($"Invalid state transition of Resume, trying to transit from {State} to {GameStateType.Playing}");
    }

    public void GameOver()
    {
        if (State == GameStateType.Playing)
        {
            ChangeState(GameStateType.GameOver);
            GameEvents.OnGameOver?.Invoke();
            Debug.Log("Game state: GameOver");
        }
        else
            Debug.LogError($"Invalid state transition of GameOver, trying to transit from {State} to {GameStateType.GameOver}");
    }

    private void ChangeState(GameStateType nextState)
    {
        var prevState = _currentState;
        _currentState = nextState;

        OnStateChange?.Invoke(prevState, nextState);
    }
}
