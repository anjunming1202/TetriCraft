using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PauseManager))]
public abstract class GameController : Singleton<GameController>
{
    public abstract GameManager GetGameManager(PlayerID playerID = PlayerID.P1);

    public abstract MapBoundaryData GetBoundaryData(PlayerID playerID = PlayerID.P1);

    public abstract PlayerInput GetPlayerInput(PlayerID playerID = PlayerID.P1);

    protected override void Awake()
    {
        base.Awake();
        // TODO: (perhaps) control all existing game managers from this single entrance

        InputRoot.EnableOutOfGameUIInput();
    }

    protected void Start()
    {
        // TODO: (perhaps) control all existing game managers from this single entrance
    }

    protected void Update()
    {
        // TODO: (perhaps) control all existing game managers from this single entrance
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        InputRoot.DisableOutOfGameUIInput();
    }

    protected virtual void OnInitialise()
    {

    }
    protected virtual void OnStartGame()
    {

    }
    protected virtual void OnQuitGame()
    {

    }
}