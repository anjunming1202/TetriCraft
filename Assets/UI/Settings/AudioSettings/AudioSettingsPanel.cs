using System;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsPanel : SettingsPanel
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider blockSoundsSlider;
    [SerializeField] private Slider eventSoundsSlider;
    [SerializeField] private Slider uiSoundsSlider;

    [SerializeField] private Button doneButton;

    protected override void Awake()
    {
        base.Awake();
        masterVolumeSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.GlobalSettings.masterVolume = value; });
        musicVolumeSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.GlobalSettings.musicVolume = value; });
        blockSoundsSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.GlobalSettings.blocksVolume = value; });
        eventSoundsSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.GlobalSettings.eventsVolume = value; });
        uiSoundsSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.GlobalSettings.uiVolume = value; });
    }

    protected override void PopulateData(SettingsData data)
    {
        PopulateSliderData(masterVolumeSlider, data.GlobalSettings.masterVolume);
        PopulateSliderData(musicVolumeSlider, data.GlobalSettings.musicVolume);
        PopulateSliderData(blockSoundsSlider, data.GlobalSettings.blocksVolume);
        PopulateSliderData(eventSoundsSlider, data.GlobalSettings.eventsVolume);
        PopulateSliderData(uiSoundsSlider, data.GlobalSettings.uiVolume);
    }
}
