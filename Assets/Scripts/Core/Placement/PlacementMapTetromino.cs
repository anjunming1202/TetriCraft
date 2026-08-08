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

    // Real shift-to-floor movement (collision-checked, same as HardDrop()) but locks immediately
    // instead of going through Ground()'s lock-delay coroutine — for fidelity checks comparing against
    // CommitPlacementInstant without needing to wait through real lock-delay timing. Built entirely
    // from MapTetromino's public simulate-drop primitives (same pattern GetLegalPlacements() already
    // uses), so HardDrop()/Ground() themselves stay untouched for the demo's real-timing path.
    public void HardDropImmediate(MapManager map)
    {
        while (true)
        {
            ShiftPending(0, -1);
            if (CheckValid(map))
                continue;
            ShiftPending(0, 1); // step back onto the last valid cell
            break;
        }

        MoveBlocksToPendingPositions(map, animation: false);
        ForceLockdown(map);
    }
}
