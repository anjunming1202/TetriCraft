using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class OptionsPanel : SettingsPanel
{
    [Header("UI Components")]
    [SerializeField] private Button audioSettingsButton;
    [SerializeField] private Button graphicsSettingsButton;
    [SerializeField] private Button accessibilitySettingsButton;
    [SerializeField] private Button controlSettingsButton;

    [SerializeField] private Button doneButton;

    protected override void Awake()
    {
        base.Awake();
        Init(GetComponent<OptionsPanelController>());
    }

    public void Init(OptionsPanelController optionsMenuController)
    {
        audioSettingsButton.onClick.RemoveAllListeners();
        graphicsSettingsButton.onClick.RemoveAllListeners();
        accessibilitySettingsButton.onClick.RemoveAllListeners();
        controlSettingsButton.onClick.RemoveAllListeners();
        doneButton.onClick.RemoveAllListeners();

        audioSettingsButton.onClick.AddListener(optionsMenuController.OnAudioSettings);
        graphicsSettingsButton.onClick.AddListener(optionsMenuController.OnGraphicsSettings);
        accessibilitySettingsButton.onClick.AddListener(optionsMenuController.OnAccessibilitySettings);
        controlSettingsButton.onClick.AddListener(optionsMenuController.OnControlsSettings);
        doneButton.onClick.AddListener(optionsMenuController.OnDone);
    }

    protected override void PopulateData(SettingsData data) { }
}
