using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PausedState : GameState
{
    public override GameStateType Type => GameStateType.Paused;

    public override UniTask Enter()
    {
        Debug.Log("Entering PausedState");
        return UniTask.CompletedTask;
    }

    public override UniTask Exit()
    {
        Debug.Log("Exiting PausedState");
        return UniTask.CompletedTask;
    }
}
