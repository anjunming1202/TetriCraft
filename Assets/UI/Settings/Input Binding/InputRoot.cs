using UnityEngine;
using UnityEngine.InputSystem;

public class InputRoot : PersistentSingleton<InputRoot>
{
    [SerializeField] private PlayerInput playerInput1;
    [SerializeField] private PlayerInput playerInput2;

    public static PlayerInput GetRootPlayerInput(PlayerID playerID) => playerID == PlayerID.P1 ? Instance.playerInput1 : Instance.playerInput2;

    protected override void Awake()
    {
        base.Awake();

        Debug.Assert(playerInput1 != null);
        Debug.Assert(playerInput2 != null);

        playerInput1.actions.Disable();
        playerInput2.actions.Disable();
    }

    public static void EnableOutOfGameUIInput()
    {
        Instance.playerInput1.actions.Enable();
        Instance.playerInput2.actions.Enable();
    }
    public static void DisableOutOfGameUIInput() 
    {
        Instance.playerInput1.actions.Disable();
        Instance.playerInput2.actions.Disable();
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
