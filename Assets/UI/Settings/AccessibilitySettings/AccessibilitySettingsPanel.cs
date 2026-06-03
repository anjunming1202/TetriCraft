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
        dropSpeedSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending[PlayerID].dropSpeed = value; });
        ghostPieceTypeButton.onValueChanged.AddListener(value => { if (Pending == null) return; Pending[PlayerID].ghostPiece = value; });
        dropAnimationSpeedSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending[PlayerID].dropAnimationSpeed = value; });
        ghostPieceOpaitySlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending[PlayerID].ghostPieceOpacity = value; });
        blockOutlinesButton.onValueChanged.AddListener(value => { if (Pending == null) return; Pending[PlayerID].blockOutlines = value == OnOff.On; });
        hardDropVibrationStrengthSlider.onValueChanged.AddListener(value => { if (Pending == null) return; Pending[PlayerID].hardDropVibrationStrength = value; });
    }

    protected override void PopulateData(SettingsData data)
    {
        Debug.Log("Accessibility Settings Populating Data");
        PopulateSliderData(dropSpeedSlider, data[PlayerID].dropSpeed);
        ghostPieceTypeButton.Value = data[PlayerID].ghostPiece;
        PopulateSliderData(dropAnimationSpeedSlider, data[PlayerID].dropAnimationSpeed);
        PopulateSliderData(ghostPieceOpaitySlider, data[PlayerID].ghostPieceOpacity);
        blockOutlinesButton.Value = data[PlayerID].blockOutlines ? OnOff.On : OnOff.Off;
        PopulateSliderData(hardDropVibrationStrengthSlider, data[PlayerID].hardDropVibrationStrength);
    }
}
