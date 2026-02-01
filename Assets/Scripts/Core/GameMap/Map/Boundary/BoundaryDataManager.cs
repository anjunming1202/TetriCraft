public static class BoundaryDataManager
{ 
    public static MapBoundaryData GetBoundaryData(PlayerID playerID)
    {
        return GameController.Instance.GetBoundaryData(playerID);
    }
}
