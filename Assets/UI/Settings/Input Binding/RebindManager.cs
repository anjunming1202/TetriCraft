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
    }

    private void OnEnable()
    {
        SettingsManager.Instance.OnSettingsChanged += LoadBindings;
    }

    private void OnDisable()
    {
        SettingsManager.Instance.OnSettingsChanged -= LoadBindings;
    }

    private void Start()
    {
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
            Debug.Log($"{gameObject.name} {playerInput.actions.name} Bindings loaded");
            playerInput.actions.LoadBindingOverridesFromJson(json);
            isLoaded = true;
        }
    }
}
