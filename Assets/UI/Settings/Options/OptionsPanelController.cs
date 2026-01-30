using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionsPanelController : MonoBehaviour
{
    public void OnAudioSettings()
    {
        UIManager.Instance.ShowPanel<AudioSettingsPanel>("AudioSettings");
    }

    public void OnGraphicsSettings()
    {
        UIManager.Instance.ShowPanel<GraphicSettingsPanel>("GraphicSettings");
    }

    public void OnAccessibilitySettings()
    {
        UIManager.Instance.ShowPanel<AccessibilitySettingsPanel>("AccessibilitySettings");
    }

    public void OnControlsSettings()
    {
        UIManager.Instance.ShowPanel<ControlSettingsPanel>("ControlSettings");
    }

    public void OnDone()
    {
        UIManager.Instance.OnBack();
    }
}