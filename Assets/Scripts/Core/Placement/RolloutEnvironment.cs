using System;
using System.Collections.Generic;
using UnityEngine;

// Rollout environment: Reset/Step/GetLegalPlacements/IsDone, driven directly and synchronously
// by whatever calls it (a manual test harness now, an IPC handler later). Owns PlacementTetrisManager
// directly — no GameController, no PlayerGameManager, no frame-loop polling.
public class RolloutEnvironment : MonoBehaviour
{
    [SerializeField] private PlacementTetrisManager tetrisManager;
    [SerializeField] private Transform boundaryRegion;
    [SerializeField] private PlayerID playerID;

    private MapBoundaryData boundaryData;
    private bool hasStartedFirstEpisode;

    public event Action<PlayerID> OnEpisodeEnded;

    public bool IsDone { get; private set; }

    private void Awake()
    {
        // BlockAnimator/BlockSoundManager check this to skip animation/sound coroutines and,
        // for sound, the UnityEngine.Random draw that would otherwise perturb the piece RNG stream.
        HeadlessRuntime.IsHeadless = true;

        tetrisManager.Initialise();

        boundaryData = MapBoundaryData.Create(boundaryRegion);
        BoundaryDataManager.Register(playerID, boundaryData);

        tetrisManager.OnGameDead += HandleBoardDead;
    }

    private void OnDestroy()
    {
        tetrisManager.OnGameDead -= HandleBoardDead;
        BoundaryDataManager.Unregister(playerID);
    }

    private void HandleBoardDead()
    {
        IsDone = true;
        OnEpisodeEnded?.Invoke(playerID);
    }

    // Bypasses GameController/PlayerGameManager entirely — those exist only for a human session.
    public void Reset(int seed)
    {
        UnityEngine.Random.InitState(seed);

        // No map exists yet before the first episode — cleaning up here would tear down
        // FluidSystem/etc. state that was never initialised (matches GameController.NewGame(),
        // which never cleans up before the first game either, only on later restarts).
        if (hasStartedFirstEpisode)
            tetrisManager.CleanUpTetrisMap();
        hasStartedFirstEpisode = true;

        tetrisManager.PrepareNewTetrisMap(boundaryData.width, boundaryData.height, playerID);
        tetrisManager.StartNewMap();

        // StartNewMap() spawns the first piece as a *pending* block-grid request (via
        // BlockSystemManager's batch) — nothing flushes it until the next OnUpdate(). Every later
        // piece gets flushed within the same Step() that spawned it, but the very first one has no
        // such call after it, so its blocks would sit unregistered until the first commit, at which
        // point BlockGridManager.ResolveRequestConflicts() resolves the spawn/move conflict in favour
        // of the (stale) spawn — silently discarding the placement move. One extra OnUpdate() here
        // flushes it immediately, before anything else can race it.
        //
        // Headless path never calls TickManager.Update(), so a prior episode's last Step()
        // can leave IsGameTickUpdate true. Clear it before the flush so RandomTickManager
        // doesn't fire and shift the new episode's RNG state.
        TickManager.ConsumeTick();
        tetrisManager.OnUpdate();

        IsDone = false;
    }

    public IReadOnlyList<PlacementCandidate> GetLegalPlacements() => tetrisManager.GetLegalPlacements();

    // tetrisManager.BoundaryWidth/Height are only populated by PrepareNewTetrisMap() inside Reset().
    // Before the first Reset (e.g. HeadlessIpcServer reads dims to send HELLO at startup) fall back
    // to boundaryData, which Awake() already computed from boundaryRegion.
    public int BoardWidth => tetrisManager.BoundaryWidth > 0
        ? tetrisManager.BoundaryWidth
        : (boundaryData != null ? boundaryData.width : 0);
    public int BoardHeight => tetrisManager.BoundaryHeight > 0
        ? tetrisManager.BoundaryHeight
        : (boundaryData != null ? boundaryData.height : 0);

    public void GetBoardOccupancy(byte[] buffer) => tetrisManager.GetBoardOccupancy(buffer);

    // Commits the placement and advances the turn synchronously — no frame-loop dependency.
    // Explicit ticks (not real-time Update()-driven) are what let tick-gated systems like
    // FluidManager's settle-into-FluidDummy step actually run between placements; without at least
    // one, TickManager.IsGameTickUpdate never goes true in a synchronous/headless run.
    //
    // tickCount defaults to 1 — the training path has no time-related mechanics once the Stage-1
    // block set (inert only) lands, so one tick per action is both correct and cheapest. Fluid flows
    // gradually (FluidManager.Flow() moves a small unitFlowingAmount per tick), so a caller that still
    // has fluid in play — the fidelity check, exercising today's full block set — needs to pass a
    // larger tickCount to let flow/settling fully resolve before the next placement, or it can catch
    // a fluid block mid-transition and see stale state that never happens in real (many-frames-per-
    // turn) gameplay.
    //
    // Returns lines cleared this turn (from totalClearLineCount, which is reset at the start of each
    // OnLockdown and accumulated by TryClearLines within that lockdown + subsequent OnUpdate passes).
    public int Step(PlacementCandidate candidate, int tickCount = 1)
    {
        tetrisManager.CommitPlacementInstant(candidate);
        for (int i = 0; i < tickCount; i++)
        {
            TickManager.AdvanceTicks(1);
            tetrisManager.OnUpdate();
        }
        return (int)tetrisManager.totalClearLineCount;
    }

    /// <summary>
    /// Single-call after-state query: for each legal placement of the current piece, compute the
    /// resulting board occupancy grid and lines cleared — without mutating any Unity state.
    /// Callers pre-allocate afterStates[maxCandidates][w*h] and linesCleared[maxCandidates].
    /// Returns the actual number of candidates populated.
    /// </summary>
    public int QueryAfterStates(byte[][] afterStates, int[] linesCleared)
    {
        var candidates = tetrisManager.GetLegalPlacements();
        int n = candidates.Count;

        int gridSize = BoardWidth * BoardHeight;
        byte[] baseGrid = new byte[gridSize];
        tetrisManager.GetBoardOccupancy(baseGrid);

        for (int i = 0; i < n; i++)
            linesCleared[i] = tetrisManager.ComputeAfterState(baseGrid, candidates[i], afterStates[i]);

        return n;
    }
}
