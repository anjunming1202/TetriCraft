using UnityEngine;
using UnityEngine.InputSystem;

public class InputRoot : PersistentSingleton<InputRoot>
{
    public PlayerInput playerInput {  get; private set; }

    protected override void Awake()
    {
        base.Awake();

        if (playerInput == null) playerInput = GetComponentInChildren<PlayerInput>();
        playerInput.actions.Disable();
    }

    public static void EnableOutOfGameUIInput() { Instance.playerInput.actions.Enable(); }
    public static void DisableOutOfGameUIInput() { Instance.playerInput.actions.Disable(); }

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
