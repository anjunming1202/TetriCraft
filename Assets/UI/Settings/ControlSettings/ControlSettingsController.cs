using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlSettingsController : SettingsPanelController
{
    public void OnKeyBinds()
    {
        UIManager.Instance.ShowPanel<RebindPanel>("KeyBinds");
    }
}
