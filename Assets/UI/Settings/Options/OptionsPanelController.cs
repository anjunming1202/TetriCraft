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

    }

    public void OnAccessibilitySettings()
    {
        UIManager.Instance.ShowPanel<AccessibilitySettingsPanel>("AccessibilitySettings", SettingsManager.Instance.Current);
    }

    public void OnControlsSettings()
    {

    }

    public void OnDone()
    {
        UIManager.Instance.OnBack();
    }
}