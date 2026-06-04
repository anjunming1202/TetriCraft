using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroState : GameState
{
    public override GameStateType Type => GameStateType.Intro;

    public override UniTask Enter()
    {
        Debug.Log("Entering IntroState");
        return UniTask.CompletedTask;
    }

    public override UniTask Exit()
    {
        Debug.Log("Exiting IntroState");
        return UniTask.CompletedTask;
    }
}
