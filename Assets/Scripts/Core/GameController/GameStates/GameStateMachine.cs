using System;
using UnityEngine;
using UnityEngine.Events;

public class GameStateMachine : StaticInstance<GameStateMachine>
{
    public static GameStates State => state;

    public event Action OnInitialised;
    public event Action OnIntro;
    public event Action OnGameStart;
    public event Action OnGamePaused;
    public event Action OnGameResumed;
    public event Action OnGameOver;

    private static GameStates state = GameStates.Loading;

    public void Init()
    {
        state = GameStates.Initialised;
        OnInitialised?.Invoke();

        Debug.Log("Game state: Initialised");
    }

    public void Intro()
    {
        if (state == GameStates.Initialised)
        {
            state = GameStates.Intro;
            OnIntro?.Invoke();
            Debug.Log("Game state: Intro");
        }
        else
            Debug.LogError($"Invalid state transition of Countdown, trying to transit from {state} to {GameStates.Intro}");
    }

    public void StartGame()
    {
        if (state == GameStates.Intro)
        {
            state = GameStates.Playing;
            OnGameStart?.Invoke();
            Debug.Log("Game state: Playing");
        }
        else
            Debug.LogError($"Invalid state transition of Start, trying to transit from {state} to {GameStates.Playing}");
    }

    public void Pause()
    {
        if (state == GameStates.Playing)
        {
            state = GameStates.Paused;
            OnGamePaused?.Invoke();
            Debug.Log("Game state: Paused");
        }
        else
        {
            Debug.LogError($"Invalid state transition of Pause, trying to transit from {state} to {GameStates.Paused}");
        }
    }

    public void Resume()
    {
        if (state == GameStates.Paused)
        {
            state = GameStates.Playing;
            OnGameResumed?.Invoke();
            Debug.Log("Game state: Playing");
        }
        else
            Debug.LogError($"Invalid state transition of Resume, trying to transit from {state} to {GameStates.Playing}");
    }

    public void GameOver()
    {
        if (state == GameStates.Playing)
        {
            state = GameStates.GameOver;
            OnGameOver?.Invoke();
            Debug.Log("Game state: GameOver");
        }
        else
            Debug.LogError($"Invalid state transition of GameOver, trying to transit from {state} to {GameStates.GameOver}");
    }
}

public enum GameStates
{
    Loading,
    Initialised,
    Intro,
    Playing,
    Paused,
    GameOver
}
