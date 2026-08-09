using System.Collections.Generic;
using UnityEngine;

// Does CommitPlacementInstant (the training path) produce the exact same board trajectory as real
// gameplay, for the same seed and the same sequence of chosen (rotation, column) decisions? Runs both
// passes synchronously in Start() and reports PASS/FAIL to the Console. No Unity Test Framework
// dependency — this project has no assembly definition boundary a separate Tests assembly could
// reference Assembly-CSharp types through (custom asmdefs compile before Assembly-CSharp, so they
// structurally can't see RolloutEnvironment/PlacementTetrisManager).
//
// Verification-only tool: holds references into RolloutEnvironment/PlacementTetrisManager, never the
// reverse — same decoupling as HeadlessManualTestHarness.
public class PlacementFidelityCheck : MonoBehaviour
{
    [SerializeField] private RolloutEnvironment environment;
    [SerializeField] private PlacementTetrisManager tetrisManager;
    [SerializeField] private int seed = 12345;
    [SerializeField] private int maxTurns = 40;

    // Ticks pumped per turn before snapshotting — mirrors RolloutEnvironment.Step's default (1),
    // since a fidelity check that uses a different tick count than training would prove the wrong
    // thing. Kept as a field only so it stays in one obvious place if that default ever changes.
    [SerializeField] private int settleTicksPerTurn = 1;

    private void Start()
    {
        // FluidManager.BlockSqueeze/SplitElement has a pre-existing bug that Debug.Assert-fails when
        // a newly-locked fluid block squeezes against fluid already mid-flow — real gameplay rarely
        // triggers it (pieces lock far apart in real time), but placements running back-to-back with
        // no natural pacing hit it reliably. Stage-1 training won't include fluid blocks anyway, so
        // excluding them here lets this check validate everything else without depending on a fix to
        // that unrelated engine bug. Mutates the shared SpawnableBlockList asset's in-memory instance
        // only for this Play session — Unity discards ScriptableObject edits made in Play Mode on exit,
        // it's never written back to disk.
        BlockRandomSelector.Instance.List.Remove(BlockID.Lava);
        BlockRandomSelector.Instance.List.Remove(BlockID.Water);

        var decisions = new List<(int rotation, int column)>();
        var instantSnapshots = new List<string>();

        environment.Reset(seed);
        for (int turn = 0; turn < maxTurns && !environment.IsDone; turn++)
        {
            var candidates = environment.GetLegalPlacements();
            if (candidates.Count == 0)
                break;

            // Any deterministic policy works — fidelity doesn't depend on decision quality, only on
            // both passes making the exact same decision at the exact same turn.
            var chosen = candidates[0];
            decisions.Add((chosen.Rotation, chosen.Column));
            environment.Step(chosen, settleTicksPerTurn);
            instantSnapshots.Add(tetrisManager.GetBoardSnapshot());
        }

        // Re-seed (same seed => same piece sequence) and replay the exact recorded decisions through
        // real wall-kick/collision movement, locking instantly instead of waiting through Ground()'s
        // lock-delay coroutine.
        environment.Reset(seed);
        var realSnapshots = new List<string>();
        foreach (var (rotation, column) in decisions)
        {
            if (environment.IsDone)
                break;

            tetrisManager.ExecutePlacement(rotation, column, immediateLockdown: true);
            for (int tick = 0; tick < settleTicksPerTurn; tick++)
            {
                TickManager.AdvanceTicks(1);
                tetrisManager.OnUpdate();
            }
            realSnapshots.Add(tetrisManager.GetBoardSnapshot());
        }

        Report(decisions, instantSnapshots, realSnapshots);
    }

    private void Report(List<(int rotation, int column)> decisions, List<string> instantSnapshots, List<string> realSnapshots)
    {
        // Compare whichever turns both passes completed, regardless of whether lengths match.
        int commonTurns = Mathf.Min(instantSnapshots.Count, realSnapshots.Count);
        for (int i = 0; i < commonTurns; i++)
        {
            if (instantSnapshots[i] != realSnapshots[i])
            {
                var (rotation, column) = decisions[i];
                Debug.LogError($"[PlacementFidelityCheck] FAILED at turn {i} " +
                                $"(placed rotation={rotation}, column={column}):\n" +
                                $"  instant: {instantSnapshots[i]}\n" +
                                $"  real:    {realSnapshots[i]}");
                return;
            }
        }

        if (instantSnapshots.Count != realSnapshots.Count)
        {
            Debug.LogError($"[PlacementFidelityCheck] FAILED — boards identical for {commonTurns} common turns " +
                            $"but episode lengths diverged: instant={instantSnapshots.Count}, real={realSnapshots.Count}. " +
                            $"One path triggered game-over earlier despite identical board states.");
            return;
        }

        Debug.Log($"[PlacementFidelityCheck] PASSED — {instantSnapshots.Count} turns, " +
                   "identical board trajectory in both execution paths.");
    }
}
