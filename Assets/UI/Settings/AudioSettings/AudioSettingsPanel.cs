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
        masterVolumeSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.masterVolume = value; });
        musicVolumeSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.musicVolume = value; });
        blockSoundsSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.blocksVolume = value; });
        eventSoundsSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.eventsVolume = value; });
        uiSoundsSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.uiVolume = value; });
    }

    protected override void PopulateData(SettingsData data)
    {
        PopulateSliderData(masterVolumeSlider, data.masterVolume);
        PopulateSliderData(musicVolumeSlider, data.musicVolume);
        PopulateSliderData(blockSoundsSlider, data.blocksVolume);
        PopulateSliderData(eventSoundsSlider, data.eventsVolume);
        PopulateSliderData(uiSoundsSlider, data.uiVolume);
    }
}
