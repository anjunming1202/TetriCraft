using UnityEngine;

public class SingleGameController : GameController
{
    [SerializeField] GameManager gameManager;

    public override MapBoundaryData GetBoundaryData(PlayerID playerID = PlayerID.P1)
    {
        return gameManager.boundaryData;
    }
}
