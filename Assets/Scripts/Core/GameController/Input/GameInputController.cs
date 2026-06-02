using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GameManager))]
public class GameInputController : MonoBehaviour
{
    protected GameManager gameManager;
    [SerializeField] protected TetrominoController controller;

    protected PlayerInput playerInput;

    protected void Awake()
    {
        gameManager = GetComponent<GameManager>();
        playerInput = controller.GetComponent<PlayerInput>();

    }

    private void OnEnable()
    {
        gameManager.OnGameStart += OnGameStart;
        gameManager.OnPause += OnGamePaused;
        gameManager.OnResume += OnGameResumed;
        gameManager.OnGameOver += OnGameOver;
    }

    private void OnDisable()
    {
        gameManager.OnGameStart -= OnGameStart;
        gameManager.OnPause -= OnGamePaused;
        gameManager.OnResume -= OnGameResumed;
        gameManager.OnGameOver -= OnGameOver;
    }

    protected virtual void OnGameStart()
    {
        playerInput.SwitchCurrentActionMap(controller.actionMapName);
    }

    protected virtual void OnGamePaused()
    {
        playerInput.SwitchCurrentActionMap("UI");
    }

    protected virtual void OnGameResumed()
    {
        playerInput.SwitchCurrentActionMap(controller.actionMapName);
    }

    protected virtual void OnGameOver()
    {
        playerInput.SwitchCurrentActionMap("UI");
    }
}
