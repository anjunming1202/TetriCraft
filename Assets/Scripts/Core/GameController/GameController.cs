public abstract class GameController : Singleton<GameController>
{
    public abstract MapBoundaryData GetBoundaryData(PlayerID playerID = PlayerID.P1);
}