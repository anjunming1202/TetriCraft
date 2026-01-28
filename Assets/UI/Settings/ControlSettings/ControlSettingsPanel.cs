using System;
using UnityEngine;
using UnityEngine.UI;

public class ControlSettingsPanel : SettingsPanel
{
    //[SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Button keyBindsPanelButton;

    [SerializeField] private Button doneButton;

    protected override void Awake()
    {
        
    }

    protected override void PopulateData(SettingsData data)
    {
        
    }
}
