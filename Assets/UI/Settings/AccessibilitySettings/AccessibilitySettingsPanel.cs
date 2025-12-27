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

    public void Init(AccessibilitySettingsController controller)
    {

    }

    protected override void PopulateData(SettingsData data)
    {
        Debug.Log("Accessibility Settings Populating Data");
        dropSpeedSlider.value = data.dropSpeed;
        ghostPieceTypeButton.Value = data.ghostPiece;
        dropAnimationSpeedSlider.value = data.dropAnimationSpeed;
        ghostPieceOpaitySlider.value = data.ghostPieceOpacity;
        blockOutlinesButton.Value = data.blockOutlines ? OnOff.On : OnOff.Off;
        hardDropVibrationStrengthSlider.value = data.hardDropVibrationStrength;
    }
}
