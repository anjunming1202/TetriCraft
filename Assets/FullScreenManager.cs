using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullScreenManager : PersistentSingleton<FullScreenManager>
{
    void Start()
    {
        SettingsManager.Instance.OnSettingsChanged += data => SetFullScreen(data.GlobalSettings.fullscreen);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
            SetFullScreen(!Screen.fullScreen);
    }

    public void SetFullScreen(bool fullscreen)
    {
        Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        SettingsManager.Current.GlobalSettings.fullscreen = fullscreen; // edit setting data directly
        Debug.Log($"Full Screen: {fullscreen}");
    }
}
