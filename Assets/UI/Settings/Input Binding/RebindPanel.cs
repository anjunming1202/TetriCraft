using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RebindPanel : SettingsPanel
{
    [SerializeField] private RebindButtonUI[] rebindButtons;

    [SerializeField] private Button doneButton;

    private PlayerInput playerInput => InputRoot.Instance.playerInput;

    protected override void Awake()
    {
        base.Awake();
        foreach (var button in rebindButtons)
        {
            button.SetPlayerInput(playerInput);
            // rebind button on changed => save to pending
            button.onBindingsUpdate.AddListener(playerInput => SaveBindings(playerInput, Pending));
        }
    }

    protected override void PopulateData(SettingsData data)
    {
        // debug check
        if (!InputRoot.Instance.rebindManager.isLoaded)
        {
            Debug.Log("Bindings not loaded to actions asset");
        }

        // populate action bindings
        foreach (var button in rebindButtons)
        {
            // update to populate when open
            button.UpdateBindingDisplay();
        }
    }

    private static void SaveBindings(PlayerInput playerInput, SettingsData settingsData)
    {
        Debug.Log("update bindings");
        var actions = playerInput.actions;
        if (actions != null)
        {
            Debug.Log("save");
            string json = actions.SaveBindingOverridesAsJson();
            settingsData.inputBindingsJson = json;
        }
    }
}
