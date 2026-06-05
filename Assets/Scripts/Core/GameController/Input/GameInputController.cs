using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerGameManager))]
public class GameInputController : MonoBehaviour
{
    protected PlayerGameManager gameManager;
    [SerializeField] protected TetrominoController controller;

    protected PlayerInput playerInput;

    protected void Awake()
    {
        gameManager = GetComponent<PlayerGameManager>();
        playerInput = controller.GetComponent<PlayerInput>();

    }

    private void OnEnable()
    {
        GameEvents.OnGameStart += OnGameStart;
        GameEvents.OnGamePaused += OnGamePaused;
        GameEvents.OnGameResumed += OnGameResumed;
        GameEvents.OnGameOver += OnGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStart -= OnGameStart;
        GameEvents.OnGamePaused -= OnGamePaused;
        GameEvents.OnGameResumed -= OnGameResumed;
        GameEvents.OnGameOver -= OnGameOver;
    }

    protected virtual void OnGameStart()
    {
        playerInput.SwitchCurrentActionMap(controller.actionMapName);

        InputSystemUtility.LogActionMaps(playerInput);
    }

    protected virtual void OnGamePaused()
    {
        playerInput.SwitchCurrentActionMap("UI");

        InputSystemUtility.LogActionMaps(playerInput);
    }

    protected virtual void OnGameResumed()
    {
        playerInput.SwitchCurrentActionMap(controller.actionMapName);

        InputSystemUtility.LogActionMaps(playerInput);
    }

    protected virtual void OnGameOver()
    {
        playerInput.SwitchCurrentActionMap("UI");
    }
}
