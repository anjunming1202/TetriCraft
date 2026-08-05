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
        tetrisManager.OnUpdate();

        IsDone = false;
    }

    public IReadOnlyList<PlacementCandidate> GetLegalPlacements() => tetrisManager.GetLegalPlacements();

    // Commits the placement and advances the turn synchronously — no frame-loop dependency.
    public void Step(PlacementCandidate candidate)
    {
        tetrisManager.CommitPlacementInstant(candidate);
        tetrisManager.OnUpdate();
    }
}
