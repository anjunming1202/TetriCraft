using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSettingsController : MonoBehaviour
{
    public void OnDone()
    {
        SettingsManager.Instance.ApplyEdit(); // if having a 'Cancel', split this to 'OnDone', 'OnCancel' and 'OnEsc'
        SettingsManager.Instance.Save();
        UIManager.Instance.OnBack();
    }
}
