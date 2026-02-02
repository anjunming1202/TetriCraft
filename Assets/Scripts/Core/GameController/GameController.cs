using UnityEngine.InputSystem;

public abstract class GameController : Singleton<GameController>
{
    public abstract MapBoundaryData GetBoundaryData(PlayerID playerID = PlayerID.P1);

    public abstract PlayerInput GetPlayerInput(PlayerID playerID = PlayerID.P1);
}