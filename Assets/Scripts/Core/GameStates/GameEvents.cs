using System;

public static class GameEvents
{
    public static Action OnInitialised;
    public static Action OnPrepareNewGame;
    public static Action OnIntroStart;
    public static Action OnGameStart;
    public static Action OnGamePaused;
    public static Action OnGameResumed;
    public static Action OnGameOver;
    public static Action OnCleanUp;
}