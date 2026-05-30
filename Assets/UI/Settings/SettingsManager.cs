using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SettingsManager : PersistentSingleton<SettingsManager>
{
    public static SettingsData Current { get; private set; }
    public static SettingsData Pending { get; private set; }// // explosed to settings controller to adjust settings
    public bool isEditting = false;

    //public event Action<GlobalSettings> OnGlobalSettingsChanged;
    public event Action<SettingsData> OnSettingsChanged; // notify all managers to change values (can have a event for each manager)

    // path for storing option settings
    private const string PREF_KEY = "settings";

    [SerializeField] private SettingsData defaultData; 

    private PlayerID modifierID;

    protected override void Awake()
    {
        base.Awake();
        Load();
        UpdateSettings(Current);
    }

    public void StartEdit(PlayerID modifier)//
    {
        this.modifierID = modifier;
        Pending = Clone(Current);
        isEditting = true;
    }

    public void CancelEdit()//
    {
        Pending = null;
        UpdateSettings(Current);
        isEditting= false;
        Debug.Log("Cancelled Editting");
    }

    public void ApplyEdit()
    {
        if (Pending != null)
        {
            Current = Clone(Pending);
            Debug.Log($"Current: {JsonUtility.ToJson(Current)}");
        }
        Pending = null;
        UpdateSettings(Current);
        Debug.Log("Applied Settings");

        Save();

        isEditting = false;
    }

    /// <summary>
    /// For real time updated features
    /// </summary>
    public void UpdatePendingSettings()//
    {
        UpdateSettings(Pending);
    }

    public void Load()
    {
        string json = PlayerPrefs.GetString(PREF_KEY, "");
        if (!string.IsNullOrEmpty(json))
            Current = JsonUtility.FromJson<SettingsData>(json);
        if (Current == null)
        {
            if (defaultData != null)
            {
                Current = Clone(defaultData);
                Debug.Log("Loaded Default Settings");
            }
            else
            {
                Debug.LogWarning("Lack of default settings data, created a new data");
                Current = new SettingsData();
            }
        }
        else
            Debug.Log("Loaded Last Saved Settings");
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(Current);
        PlayerPrefs.SetString(PREF_KEY, json);
        PlayerPrefs.Save();
    }

    private void UpdateSettings(SettingsData settings)
    {
        //OnGlobalSettingsChanged?.Invoke(settings.GlobalSettings);
        OnSettingsChanged?.Invoke(settings);
    }

    private SettingsData Clone(SettingsData src)
    {
        string json = JsonUtility.ToJson(src);
        return JsonUtility.FromJson<SettingsData>(json);
    }
}
