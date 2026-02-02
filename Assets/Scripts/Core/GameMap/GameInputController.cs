using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInputController : MonoBehaviour
{
    private GameManager gameManager;
    [SerializeField] private TetrominoController controller;

    private PlayerInput playerInput;

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
        playerInput = controller.GetComponent<PlayerInput>();

    }

    private void OnEnable()
    {
        gameManager.OnGameStart += () => playerInput.SwitchCurrentActionMap(controller.actionMapName);
        gameManager.OnPause += () => playerInput.SwitchCurrentActionMap("UI");
        gameManager.OnResume += () => playerInput.SwitchCurrentActionMap(controller.actionMapName);
        gameManager.OnGameOver += () => playerInput.SwitchCurrentActionMap("UI");
    }

    private void OnDisable()
    {
        gameManager.OnGameStart -= () => playerInput.SwitchCurrentActionMap(controller.actionMapName);
        gameManager.OnPause -= () => playerInput.SwitchCurrentActionMap("UI");
        gameManager.OnResume -= () => playerInput.SwitchCurrentActionMap(controller.actionMapName);
        gameManager.OnGameOver -= () => playerInput.SwitchCurrentActionMap("UI");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!gameManager.IsPaused)
                gameManager.PauseGame();
            else
                gameManager.ResumeGame();
        }
    }
}
