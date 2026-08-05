/// <summary>
/// Exposes the instant-lock primitive the training/headless path needs: lock the piece into the
/// board right now, with no lock-delay wait. Unlike the demo/gameplay-fidelity path (which uses the
/// real HardDrop()/Ground() and its lock-delay coroutine), the training path already knows the exact
/// landing cell from GetLegalPlacements() and teleports there directly (see
/// PlacementTetrisManager.CommitPlacementInstant) — it never calls Ground(), so it needs to reach
/// Lockdown() without going through the coroutine at all.
/// </summary>
public class PlacementMapTetromino : MapTetromino
{
    public void ForceLockdown(MapManager map) => Lockdown(map);
}
