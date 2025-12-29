using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public abstract class MenuPanel : BasePanel
{
    protected override void Awake()
    {
        base.Awake();
        SetButtonClickSounds();
    }

    private void SetButtonClickSounds()
    {
        foreach (var button in GetComponentsInChildren<Button>())
        {
            /*button.onClick.RemoveAllListeners();
            button.onClick.AddListener(UIAudioManager.Instance.Play);*/

            if (!button.TryGetComponent<ButtonPointerDownListener>(out var buttonDown))
                buttonDown = button.AddComponent<ButtonPointerDownListener>();
            //Debug.Log(buttonDown);
            buttonDown.onDown.RemoveAllListeners();
            buttonDown.onDown.AddListener(UIAudioManager.Instance.Play);
        }
    }
}
