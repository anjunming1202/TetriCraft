using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleGameController : GameController
{
    [SerializeField] PlayerGameManager gameManagerP1;
    [SerializeField] PlayerGameManager gameManagerP2;

    [SerializeField] PlayerInput controllerPlayerInputP1;
    [SerializeField] PlayerInput controllerPlayerInputP2;

    public override PlayerGameManager GetGameManager(PlayerID playerID)
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

    protected override void Initialise()
    {
        throw new System.NotImplementedException();
    }

    protected override UniTask NewGame()
    {
        throw new System.NotImplementedException();
    }

    protected override UniTask InitNewGame()
    {
        throw new System.NotImplementedException();
    }

    protected override UniTask IntroGame()
    {
        throw new System.NotImplementedException();
    }

    protected override void StartGame()
    {
        throw new System.NotImplementedException();
    }

    protected override void PlayingUpdate()
    {
        throw new System.NotImplementedException();
    }

    public override void PauseGame()
    {
        throw new System.NotImplementedException();
    }

    public override void ResumeGame()
    {
        throw new System.NotImplementedException();
    }

    protected override void GameOver()
    {
        throw new System.NotImplementedException();
    }

    protected override void CleanUp()
    {
        throw new System.NotImplementedException();
    }
}
