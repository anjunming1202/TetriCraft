using UnityEngine;
using UnityEngine.InputSystem;

public class SingleGameController : GameController
{
    [SerializeField] GameManager gameManager;
    [SerializeField] PlayerInput controllerPlayerInput;

    public override MapBoundaryData GetBoundaryData(PlayerID playerID = PlayerID.P1)
    {
        return gameManager.boundaryData;
    }

    public override PlayerInput GetPlayerInput(PlayerID playerID = PlayerID.P1)
    {
        return controllerPlayerInput;
    }
}
