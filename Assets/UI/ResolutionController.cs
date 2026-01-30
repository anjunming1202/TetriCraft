using UnityEngine;

public class ResolutionController : PersistentSingleton<ResolutionController>
{
    public static Resolution current;
    public static Resolution defaultResolution;
    public static int optionCount; // available resolutions + auto option

    private static Resolution[] resolutions;

    public static Resolution GetResolution(int index)
    {
        if (index >= optionCount)
            return resolutions[optionCount - 1];

        if (index == 0)
            return defaultResolution; // auto
        else
            return resolutions[index - 1]; // available resolution
    }

    protected override void Awake()
    {
        base.Awake();
        resolutions = Screen.resolutions;
        optionCount = resolutions.Length + 1;
        defaultResolution = resolutions[optionCount - 2];
        UpdateResolution(GetResolution(SettingsManager.Instance.Current.resolutionIndex));
    }

    private void Start()
    {
        SettingsManager.Instance.OnSettingsChanged += data => UpdateResolution(GetResolution(data.resolutionIndex));
    }

    private void UpdateResolution(Resolution resolution)
    {
        current = resolution;
        // set resolution + full screen mode
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode, resolution.refreshRateRatio);
        // refresh canvas scaler
        CanvasScaleController.Instance.Refresh();
        // refresh camera
    }
}
