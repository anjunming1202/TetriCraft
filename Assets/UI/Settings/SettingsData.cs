using System;
using UnityEngine;

[Serializable]
public class SettingsData
{
    // Version for migration
    public int version = 1;

    // Audio
    public float masterVolume = 1f;   // 0..1
    public float musicVolume = 1f;
    public float blocksVolume = 1f;
    public float environmentVolume = 1f;
    public float eventsVolume = 1f;
    public float uiVolume = 1f;

    // Video / Graphics
    public int resolutionIndex = 0;   // index into available resolutions
    public bool fullscreen = true;
    //public int qualityLevel = 2;      // 0..n (Unity QualitySettings)
    public int guiScaleIndex = 1;      // index into UI scale factors for Canvas Scaler

    // Controls
    public string inputBindingsJson = ""; // serialized Input System bindings or custom map

    // Gameplay / Misc
    public float dropSpeed = 1f;
    public GhostPieceType ghostPiece = GhostPieceType.Shape;
    public float ghostPieceOpacity = 0.5f; // 0..1
    public bool blockOutlines = false;
    public float dropAnimationSpeed = 1f;
    public float hardDropVibrationStrength = 0.5f;

    // Localization
    public string locale = "en";
}
