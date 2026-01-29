using UnityEngine;
using UnityEngine.InputSystem;

public class InputRoot : PersistentSingleton<InputRoot>
{
    public PlayerInput playerInput;

    protected override void Awake()
    {
        base.Awake();
        //if (playerInput == null) Debug.LogError("PlayerInput not set in InputRoot");
    }
}
