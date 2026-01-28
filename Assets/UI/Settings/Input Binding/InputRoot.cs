using UnityEngine;
using UnityEngine.InputSystem;

public class InputRoot : PersistentSingleton<InputRoot>
{
    public PlayerInput playerInput;
    public RebindManager rebindManager;

    protected override void Awake()
    {
        base.Awake();
        if (playerInput == null) Debug.LogError("PlayerInput not set in InputRoot");
        if (rebindManager == null) Debug.LogError("RebindManager not set in InputRoot");
    }
}
