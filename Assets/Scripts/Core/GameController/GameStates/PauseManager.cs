using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PauseMenuController))]
public class PauseManager : MonoBehaviour
{
    [Tooltip("The GameManager you want to pause running when the pause key is pressed")]
    [SerializeField] private GameManager[] gameManagers;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private InputActionReference pauseActionRef;
    [SerializeField] private InputActionReference resumeActionRef;

    private InputAction pauseAction;
    private InputAction resumeAction;

    private PauseMenuPanel pauseMenuPanel;
    private PauseMenuController pauseMenuController; // to init pause panel

    private void Awake()
    {
        pauseMenuController = GetComponent<PauseMenuController>();
        pauseAction = InputSystemUtility.GetRuntimeAction(playerInput, pauseActionRef);
        resumeAction = InputSystemUtility.GetRuntimeAction(playerInput, resumeActionRef);
    }

    private void OnEnable()
    {
        pauseAction.performed += (ctx) => OnPauseTriggered();
        resumeAction.performed += (ctx) => OnResumeTriggered();
    }

    private void OnDisable()
    {
        pauseAction.performed -= (ctx) => OnPauseTriggered();
        resumeAction.performed -= (ctx) => OnResumeTriggered();
    }

    // Handle pause
    public void OnPauseTriggered()
    {
        if (gameManagers == null || gameManagers.Length == 0)
            return;

        foreach (var gameManager in gameManagers)
            if (!gameManager.IsPaused)
                OnPause(gameManager);

        Debug.Log("OnPauseTriggered");
    }

    // Handle resume
    public void OnResumeTriggered()
    {
        if (gameManagers == null || gameManagers.Length == 0)
            return;

        foreach (var gameManager in gameManagers)
            if (gameManager.IsPaused)
                OnResume(gameManager);

        Debug.Log("OnResumeTriggered");
    }

    private void OnPause(GameManager gameManager)
    {
        // pause game
        gameManager.PauseGame();

        // pause panel shown => return
        if (pauseMenuPanel != null && pauseMenuPanel.IsShown) 
            return;
        // show pause panel
        pauseMenuPanel = UIManager.Instance.ShowPanel<PauseMenuPanel>("PauseMenu");
        pauseMenuPanel.Init(pauseMenuController); // init for dynamic panel
    }

    private void OnResume(GameManager gameManager)
    {
        // other modal panel shown => return
        if (UIManager.Instance.ModalCount > 1) // pause panel is modal
            return;
        // resume game
        gameManager.ResumeGame();

        // pause panel not shown => return
        if (pauseMenuPanel == null || !pauseMenuPanel.IsShown) 
            return;
        // hide pause panel
        UIManager.Instance.HidePanel("PauseMenu");
    }
}
