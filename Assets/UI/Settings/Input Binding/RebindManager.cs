using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class RebindManager : MonoBehaviour
{
    public PlayerID playerID;
    public bool isLoaded = false;

    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        //SettingsManager.Instance.OnSettingsChanged += LoadBindings;
    }

    private void OnDisable()
    {
        SettingsManager.Instance.OnSettingsChanged -= LoadBindings;
    }

    private void Start()
    {
        SettingsManager.Instance.OnSettingsChanged += LoadBindings;

        Debug.Assert(playerInput != null, $"{gameObject.name} PlayerID not set!");
        LoadBindings(SettingsManager.Current); // load
    }

    private void LoadBindings(SettingsData settingsData)
    {
        //Debug.Log($"try load bindings for {playerID}");
        string json = settingsData[playerID].inputBindingsJson;
        Debug.Log(json);
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log($"Input bindings json has not been saved yet for {playerID}");
        }
        else
        {
            Debug.Log($"{gameObject.name} {playerInput.actions.name} Bindings loaded for {playerID}");
            playerInput.actions.LoadBindingOverridesFromJson(json);
            isLoaded = true;
        }
    }
}
