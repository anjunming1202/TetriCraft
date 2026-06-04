using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitState : GameState
{
    public override GameStateType Type => GameStateType.Initialised;

    public override UniTask Enter()
    {
        Debug.Log("Entering InitState");
        return UniTask.CompletedTask;
    }

    public override UniTask Exit()
    {
        Debug.Log("Exiting InitState");
        return UniTask.CompletedTask;
    }
}
