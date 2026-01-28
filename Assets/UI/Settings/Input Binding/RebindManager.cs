using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class RebindManager : MonoBehaviour
{
    public bool isLoaded = false;

    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        SettingsManager.Instance.OnSettingsChanged += LoadBindings;
        LoadBindings(SettingsManager.Instance.Current); // load
    }

    private void LoadBindings(SettingsData settingsData)
    {
        Debug.Log("try load bindings");
        string json = settingsData.inputBindingsJson;
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("Input bindings json has not been saved yet");
        }
        else
        {
            Debug.Log("Bindings loaded");
            playerInput.actions.LoadBindingOverridesFromJson(json);
            isLoaded = true;
        }
    }
}
