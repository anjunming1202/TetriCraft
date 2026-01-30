using UnityEngine;
using UnityEngine.UI;

public class PauseMenuPanel : MenuPanel
{
    [Header("UI Components")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button saveAndExitButton;

    public void Init(PauseMenuController pauseManuController)
    {
        resumeButton.onClick.AddListener(pauseManuController.OnResumeGame);
        settingsButton.onClick.AddListener(pauseManuController.OnSettings);
        saveAndExitButton.onClick.AddListener(pauseManuController.OnSaveAndExit);
    }
}
