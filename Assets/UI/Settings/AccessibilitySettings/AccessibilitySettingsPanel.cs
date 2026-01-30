using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AccessibilitySettingsPanel : SettingsPanel
{
    [Header("UI Components")]
    [SerializeField] private Slider dropSpeedSlider;
    [SerializeField] private ToggleButton<GhostPieceType> ghostPieceTypeButton;
    [SerializeField] private Slider dropAnimationSpeedSlider;
    [SerializeField] private Slider ghostPieceOpaitySlider;
    [SerializeField] private ToggleButton<OnOff> blockOutlinesButton;
    [SerializeField] private Slider hardDropVibrationStrengthSlider;

    [SerializeField] private Button doneButton;

    protected override void Awake()
    {
        base.Awake();
        dropSpeedSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.dropSpeed = value; });
        ghostPieceTypeButton.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.ghostPiece = value; });
        dropAnimationSpeedSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.dropAnimationSpeed = value; });
        ghostPieceOpaitySlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.ghostPieceOpacity = value; });
        blockOutlinesButton.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.blockOutlines = value == OnOff.On; });
        hardDropVibrationStrengthSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending.hardDropVibrationStrength = value; });
    }

    protected override void PopulateData(SettingsData data)
    {
        Debug.Log("Accessibility Settings Populating Data");
        PopulateSliderData(dropSpeedSlider, data.dropSpeed);
        ghostPieceTypeButton.Value = data.ghostPiece;
        PopulateSliderData(dropAnimationSpeedSlider, data.dropAnimationSpeed);
        PopulateSliderData(ghostPieceOpaitySlider, data.ghostPieceOpacity);
        blockOutlinesButton.Value = data.blockOutlines ? OnOff.On : OnOff.Off;
        PopulateSliderData(hardDropVibrationStrengthSlider, data.hardDropVibrationStrength);
    }
}
