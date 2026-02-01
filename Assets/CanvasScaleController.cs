using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasScaleController : StaticInstance<CanvasScaleController>
{
    private CanvasScaler scaler;
    private GUIScale mode;

    protected override void Awake()
    {
        base.Awake();
        scaler = GetComponent<CanvasScaler>();
    }

    private void Start()
    {
        SettingsManager.Instance.OnSettingsChanged += data => ChangeGUIScale(data.GlobalSettings.guiScale);
    }

    public void ChangeGUIScale(GUIScale mode)
    {
        this.mode = mode;
        switch (mode)
        {
            case GUIScale.Auto:
                ConfigureAutoScale();
                break;
            case GUIScale.Size1:
                ConfigureAutoScale(2f);
                break;
            case GUIScale.Size2:
                ConfigureAutoScale(1.5197f);
                break;
            case GUIScale.Size3:
                ConfigureAutoScale(1.1547f);
                break;
            case GUIScale.Size4:
                ConfigureAutoScale(0.8774f);
                break;
            case GUIScale.Size5:
                ConfigureAutoScale(0.6667f);
                break;
        }
    }

    public void Refresh()
    {
        ChangeGUIScale(mode);
    }

    private void ConfigureAutoScale()
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // Use current screen resolution as reference resolution
        if (ResolutionController.Instance == null)
            Debug.LogError("Missing ResolutionController");
        scaler.referenceResolution = new Vector2(ResolutionController.defaultResolution.width, ResolutionController.defaultResolution.height); //

        // Balanced scaling between width and height
        scaler.matchWidthOrHeight = 0.5f;

        // Recommended defaults
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.referencePixelsPerUnit = 100f;
    }

    private void ConfigureAutoScale(float resolutionScale)
    {
        ConfigureAutoScale();
        scaler.referenceResolution *= resolutionScale;
    }

    private void ChangeFixedScale(float scale)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        scaler.scaleFactor = scale;

        scaler.referencePixelsPerUnit = 100f;
    }
}
