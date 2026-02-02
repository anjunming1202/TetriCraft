using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RebindPanel2P : SettingsPanel
{
    [SerializeField] private RebindSubpanel RebindSubpanelP1;
    [SerializeField] private RebindSubpanel RebindSubpanelP2;
    [SerializeField] private Button doneButton;

    protected override void PopulateData(SettingsData data)
    {
        
    }
}
