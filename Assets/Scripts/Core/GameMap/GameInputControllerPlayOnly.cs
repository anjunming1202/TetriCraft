using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GameManager))]
public class GameInputControllerPlayOnly : GameInputController
{
    protected override void OnGamePaused()
    {
        playerInput.currentActionMap.Disable();
    }
    protected override void OnGameResumed()
    {
        playerInput.currentActionMap.Enable();
    }
    protected override void OnGameOver()
    {
        playerInput.currentActionMap.Disable();
    }
}
