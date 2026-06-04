using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverState : GameState
{
    public override GameStateType Type => GameStateType.GameOver;

    public override UniTask Enter()
    {
        Debug.Log("Entering GameOverState");
        return UniTask.CompletedTask;
    }

    public override UniTask Exit()
    {
        Debug.Log("Exiting GameOverState");
        return UniTask.CompletedTask;
    }
}
