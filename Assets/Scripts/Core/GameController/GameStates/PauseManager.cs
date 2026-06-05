using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PauseMenuController))]
public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameController gameController;
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
        pauseAction.performed += OnPauseTriggered;
        resumeAction.performed += OnResumeTriggered;
    }

    private void OnDisable()
    {
        pauseAction.performed -= OnPauseTriggered;
        resumeAction.performed -= OnResumeTriggered;
    }

    // Handle pause
    private void OnPauseTriggered()
    {
        if (!gameController.IsPaused)
                OnPause();

        Debug.Log("OnPauseTriggered");
    }
    private void OnPauseTriggered(InputAction.CallbackContext ctx) => OnPauseTriggered();

    // Handle resume
    public void OnResumeTriggered()
    {
        if (gameController.IsPaused)
            OnResume();

        Debug.Log("OnResumeTriggered");
    }
    private void OnResumeTriggered(InputAction.CallbackContext ctx) => OnResumeTriggered();

    private void OnPause()
    {
        // pause game
        gameController.PauseGame();

        // pause panel shown => return
        if (pauseMenuPanel != null && pauseMenuPanel.IsShown) 
            return;
        // show pause panel
        pauseMenuPanel = UIManager.Instance.ShowPanel<PauseMenuPanel>("PauseMenu");
        pauseMenuPanel.Init(pauseMenuController); // init for dynamic panel
    }

    private void OnResume()
    {
        // other modal panel shown => return
        if (UIManager.Instance.ModalCount > 1) // pause panel is modal
            return;

        // resume game
        gameController.ResumeGame();

        // pause panel not shown => return
        if (pauseMenuPanel == null || !pauseMenuPanel.IsShown) 
            return;
        // hide pause panel
        UIManager.Instance.HidePanel("PauseMenu");
    }
}
