using System.Collections.Generic;

// Relays to GameController when one exists (Single/Battle); falls back to a registry
// RolloutEnvironment fills in for headless scenes, which have no GameController at all.
public static class BoundaryDataManager
{
    private static readonly Dictionary<PlayerID, MapBoundaryData> boundaryData = new();

    public static void Register(PlayerID playerID, MapBoundaryData data) => boundaryData[playerID] = data;
    public static void Unregister(PlayerID playerID) => boundaryData.Remove(playerID);

    public static MapBoundaryData GetBoundaryData(PlayerID playerID) =>
        GameController.Instance != null ? GameController.Instance.GetBoundaryData(playerID) : boundaryData[playerID];
}
