using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RebindSubpanel : SettingsPanel
{
    [SerializeField] private RebindButtonUI[] rebindButtons;

    private PlayerInput playerInput;

    protected override void Awake()
    {
        base.Awake();
        playerInput = InputRoot.GetRootPlayerInput(PlayerID); // persistent PlayerInput => just for a actions template to populate, modify, and save bindings
        foreach (var button in rebindButtons)
        {
            // init
            button.Init(playerInput);

            // rebind button on changed => save to pending, check conflicts
            button.onBindingsUpdate.AddListener(() => SaveBindings(playerInput, Pending));
            button.onBindingsUpdate.AddListener(CheckConflicts);
        }
    }

    protected override void PopulateData(SettingsData data)
    {
        // populate action bindings
        foreach (var button in rebindButtons)
        {
            // update to populate when open
            button.UpdateBindingDisplay();
        }

        // check conflicts
        CheckConflicts();
    }

    private void SaveBindings(PlayerInput playerInput, SettingsData settingsData)
    {
        //Debug.Log("update bindings");
        var actions = playerInput.actions;
        if (actions != null)
        {
            string json = actions.SaveBindingOverridesAsJson();
            settingsData[PlayerID].inputBindingsJson = json;
            Debug.Log($"Saved bindings: PlayerID: {PlayerID}, Pending: {Pending[PlayerID].inputBindingsJson}");
        }
    }

    private void CheckConflicts()
    {
        HashSet<RebindButtonUI> conflictsBindingUI = new HashSet<RebindButtonUI>();
        Dictionary<string, RebindButtonUI> buttonSet = new Dictionary<string, RebindButtonUI>();

        // find conflicted bindings
        foreach (var button in rebindButtons)
        {
            if (buttonSet.ContainsKey(button.bindingText.text))
            {
                conflictsBindingUI.Add(buttonSet[button.bindingText.text]);
                conflictsBindingUI.Add(button);
            }
            else
                buttonSet.Add(button.bindingText.text, button);
        }
        // highlight conflicted bindings
        foreach (var button in rebindButtons)
        {
            if (conflictsBindingUI.Contains(button))
                button.bindingText.color = Color.yellow;
            else
                button.bindingText.color = Color.white;
        }
    }
}
