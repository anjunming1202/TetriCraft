using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphicSettingsController : MonoBehaviour
{
    public void OnDone()
    {
        SettingsManager.Instance.ApplyEdit(); // if having a 'Cancel', split this to 'OnDone', 'OnCancel' and 'OnEsc'
        SettingsManager.Instance.Save();
        UIManager.Instance.OnBack();
    }

    public void ApplyFullScreenMode(OnOff onOff)
    {
        FullScreenManager.Instance.SetFullScreen(onOff == OnOff.On);
    }

    public void ApplyGUIScale(GUIScale mode)
    {
        CanvasScaleController.Instance.ChangeGUIScale(mode);
    }
}
