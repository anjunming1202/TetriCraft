using UnityEngine;
using UnityEngine.InputSystem;

public class BattleGameController : GameController
{
    [SerializeField] GameManager gameManagerP1;
    [SerializeField] GameManager gameManagerP2;

    [SerializeField] PlayerInput controllerPlayerInputP1;
    [SerializeField] PlayerInput controllerPlayerInputP2;

    public override GameManager GetGameManager(PlayerID playerID)
    {
        return playerID == PlayerID.P1 ? gameManagerP1 : gameManagerP2;
    }

    public override MapBoundaryData GetBoundaryData(PlayerID playerID)
    {
        return playerID == PlayerID.P1 ? gameManagerP1.boundaryData : gameManagerP2.boundaryData;
    }

    public override PlayerInput GetPlayerInput(PlayerID playerID = PlayerID.P1)
    {
        return playerID == PlayerID.P1 ? controllerPlayerInputP1 : controllerPlayerInputP2;
    }
}
