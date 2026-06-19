using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayingState : GameState
{
    public override GameStateType Type => GameStateType.Playing;

    public override UniTask Enter()
    {
        Debug.Log("Entering PlayingState");
        return UniTask.CompletedTask;
    }

    public override UniTask Exit()
    {
        Debug.Log("Exiting PlayingState");
        return UniTask.CompletedTask;
    }
}
