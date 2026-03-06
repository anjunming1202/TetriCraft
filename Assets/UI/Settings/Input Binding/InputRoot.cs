using UnityEngine;
using UnityEngine.InputSystem;

public class InputRoot : PersistentSingleton<InputRoot>
{
    public PlayerInput playerInput;

    protected override void Awake()
    {
        base.Awake();
        //if (playerInput == null) Debug.LogError("PlayerInput not set in InputRoot");
        playerInput.actions.Disable();
    }

    // test
    /*private void Update()
    {
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            if (playerInput.actions.enabled)
                playerInput.actions.Disable();
            else
                playerInput.actions.Enable();
        }
    }*/
}
