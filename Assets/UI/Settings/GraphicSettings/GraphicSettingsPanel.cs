using System;
using UnityEngine;
using UnityEngine.UI;

public class GraphicSettingsPanel : SettingsPanel
{
    //[SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private ToggleSlider resolutionIndexSlider;
    [SerializeField] private ToggleButton<OnOff> fullScreenButton;
    [SerializeField] private ToggleButton<GUIScale> guiScaleButton;

    [SerializeField] private Button doneButton;

    protected override void Awake()
    {
        base.Awake();
        resolutionIndexSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.GlobalSettings.resolutionIndex = value; });
        fullScreenButton.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.GlobalSettings.fullscreen = value == OnOff.On; });
        guiScaleButton.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.GlobalSettings.guiScale = value; });
    }

    protected override void PopulateData(SettingsData data)
    {
        resolutionIndexSlider.Value = data.GlobalSettings.resolutionIndex;
        fullScreenButton.Value = data.GlobalSettings.fullscreen ? OnOff.On : OnOff.Off;
        guiScaleButton.Value = data.GlobalSettings.guiScale;
    }
}
