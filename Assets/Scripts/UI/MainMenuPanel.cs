using UnityEngine;
using UnityEngine.UI;

public class MainMenuPanel : BasePanel
{
    [Header("UI Components")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [SerializeField] private RectTransform menuContainer;

    [SerializeField] private Text versionText;
    private string versionString = "v1.1";

    protected override void Awake()
    {
        base.Awake();
        SetInteractable(false);
    }

    protected override void OnOpen(object data)
    {
        base.OnOpen(data);
        versionText.text = versionString;
        SetInteractable(true);
    }

    private void SetInteractable(bool interactable)
    {
        newGameButton.interactable = interactable;
        continueButton.interactable = interactable;
        settingsButton.interactable = interactable;
        quitButton.interactable = interactable;
    }
}
