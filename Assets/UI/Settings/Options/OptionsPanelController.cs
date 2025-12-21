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

    }

    public void OnControlsSettings()
    {

    }

    public void OnDone()
    {
        UIManager.Instance.OnBack();
    }
}