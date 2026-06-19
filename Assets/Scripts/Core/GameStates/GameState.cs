using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Cysharp.Threading.Tasks;

public abstract class GameState
{
    public abstract GameStateType Type { get; }
    public abstract UniTask Enter();
    public abstract UniTask Exit();

}

public enum GameStateType
{
    Loading,
    Initialised,
    PreGamePrepared,
    Intro,
    Playing,
    Paused,
    GameOver
}
