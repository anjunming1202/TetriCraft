using UnityEngine;

public class BattleGameController : GameController
{
    [SerializeField] GameManager gameManager1P;
    [SerializeField] GameManager gameManager2P;

    public override MapBoundaryData GetBoundaryData(PlayerID playerID)
    {
        return playerID == PlayerID.P1 ? gameManager1P.boundaryData : gameManager2P.boundaryData;
    }
}
