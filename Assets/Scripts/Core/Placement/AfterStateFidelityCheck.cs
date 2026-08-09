using UnityEngine;

// Verifies that ComputeAfterState (pure byte-array math, no Unity mutation) produces the exact same
// board occupancy as actually committing the placement via Step() and reading the real board back.
// Runs synchronously in Start() and reports PASS/FAIL to the Console. Same verification-only,
// one-directional-dependency pattern as PlacementFidelityCheck.
public class AfterStateFidelityCheck : MonoBehaviour
{
    [SerializeField] private RolloutEnvironment environment;
    [SerializeField] private PlacementTetrisManager tetrisManager;
    [SerializeField] private int seed = 12345;
    [SerializeField] private int maxTurns = 40;

    private void Start()
    {
        // Stage-1 block set only — same fluid-bug workaround as PlacementFidelityCheck.
        BlockRandomSelector.Instance.List.Remove(BlockID.Lava);
        BlockRandomSelector.Instance.List.Remove(BlockID.Water);

        environment.Reset(seed);

        int w = environment.BoardWidth;
        int h = environment.BoardHeight;
        int gridSize = w * h;

        // Pre-allocate buffers sized to the maximum possible candidate count.
        // Upper bound: 4 rotations * (boundaryWidth + pieceSize + 1) columns each — generous.
        int maxCandidates = 4 * (w + 5);
        byte[][] afterStates = new byte[maxCandidates][];
        for (int i = 0; i < maxCandidates; i++)
            afterStates[i] = new byte[gridSize];
        int[] linesClearedPredicted = new int[maxCandidates];

        byte[] realBoard = new byte[gridSize];
        int turnsChecked = 0;

        for (int turn = 0; turn < maxTurns && !environment.IsDone; turn++)
        {
            int n = environment.QueryAfterStates(afterStates, linesClearedPredicted);
            if (n == 0)
                break;

            // Pick the first candidate (deterministic, same as PlacementFidelityCheck).
            var candidates = environment.GetLegalPlacements();
            var chosen = candidates[0];
            byte[] predicted = afterStates[0];
            int predictedLines = linesClearedPredicted[0];

            // Commit the placement for real.
            int actualLines = environment.Step(chosen);

            // Read back the real board.
            tetrisManager.GetBoardOccupancy(realBoard);

            // Compare predicted vs actual board.
            bool boardMatch = true;
            for (int j = 0; j < gridSize; j++)
            {
                if (predicted[j] != realBoard[j])
                {
                    boardMatch = false;
                    break;
                }
            }

            if (!boardMatch)
            {
                Debug.LogError($"[AfterStateFidelityCheck] FAILED at turn {turn} — " +
                               $"after-state grid does not match real board after Step().\n" +
                               $"  candidate: rotation={chosen.Rotation}, column={chosen.Column}, landingY={chosen.LandingY}\n" +
                               FormatGridDiff(predicted, realBoard, w, h));
                return;
            }

            if (predictedLines != actualLines)
            {
                Debug.LogError($"[AfterStateFidelityCheck] FAILED at turn {turn} — " +
                               $"predicted {predictedLines} lines cleared but Step() returned {actualLines}.\n" +
                               $"  candidate: rotation={chosen.Rotation}, column={chosen.Column}, landingY={chosen.LandingY}");
                return;
            }

            turnsChecked++;
        }

        Debug.Log($"[AfterStateFidelityCheck] PASSED — {turnsChecked} turns, " +
                   "after-state grids and line counts match real commits exactly.");
    }

    private static string FormatGridDiff(byte[] predicted, byte[] actual, int w, int h)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("  Row-by-row (predicted | actual), bottom-to-top:");
        for (int y = 0; y < h; y++)
        {
            bool rowDiffers = false;
            for (int x = 0; x < w; x++)
                if (predicted[y * w + x] != actual[y * w + x]) { rowDiffers = true; break; }

            if (!rowDiffers) continue;

            sb.Append($"  y={y,2}: ");
            for (int x = 0; x < w; x++) sb.Append(predicted[y * w + x]);
            sb.Append(" | ");
            for (int x = 0; x < w; x++) sb.Append(actual[y * w + x]);
            sb.Append(" <-- DIFF");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
