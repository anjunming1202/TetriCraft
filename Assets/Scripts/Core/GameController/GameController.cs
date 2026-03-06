using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PauseManager))]
public abstract class GameController : Singleton<GameController>
{
    public abstract GameManager GetGameManager(PlayerID playerID = PlayerID.P1);

    public abstract MapBoundaryData GetBoundaryData(PlayerID playerID = PlayerID.P1);

    public abstract PlayerInput GetPlayerInput(PlayerID playerID = PlayerID.P1);

    /*public abstract void OnInitialise();
    public abstract void OnStartGame();
    public abstract void OnQuitGame();*/
}