using System.Collections;
using Unity.Sentis;
using UnityEngine;

/// <summary>
/// Drives RolloutEnvironment using the value network: for each step, enumerates all legal
/// after-states, evaluates them in a single batched forward pass, and picks argmax V(s').
/// Drop-in replacement for a random policy once trained weights are available.
/// </summary>
public class NeuralNetPolicyDriver : MonoBehaviour
{
    [SerializeField] private RolloutEnvironment environment;
    [SerializeField] private ModelAsset valueNetModel;
    [SerializeField] private int seed = 42;
    [SerializeField] private int maxSteps = 5000;

    // Pre-allocated buffers (sized on first use)
    private byte[][] afterStates;
    private int[] linesCleared;
    private float[] values;
    private int maxCandidates;

    private void Start()
    {
        ValueNetInference.LoadModel(valueNetModel);
        environment.Reset(seed);
        StartCoroutine(PlayLoop());
    }

    private void OnDestroy()
    {
        ValueNetInference.Dispose();
    }

    private void EnsureBuffers(int width, int height)
    {
        if (afterStates != null) return;

        // Typical Tetris has at most ~40 legal placements per piece
        maxCandidates = 80;
        int boardSize = width * height;
        afterStates = new byte[maxCandidates][];
        for (int i = 0; i < maxCandidates; i++)
            afterStates[i] = new byte[boardSize];
        linesCleared = new int[maxCandidates];
        values = new float[maxCandidates];
    }

    private IEnumerator PlayLoop()
    {
        int w = environment.BoardWidth;
        int h = environment.BoardHeight;
        EnsureBuffers(w, h);

        int steps = 0;
        int totalLines = 0;

        while (!environment.IsDone && steps < maxSteps)
        {
            int n = environment.QueryAfterStates(afterStates, linesCleared);
            if (n == 0) break;

            // Batch-evaluate all after-states
            ValueNetInference.EvaluateBatch(afterStates, values, n, w, h);

            // Argmax
            int bestIdx = 0;
            for (int i = 1; i < n; i++)
            {
                if (values[i] > values[bestIdx])
                    bestIdx = i;
            }

            // Commit the best placement
            var candidates = environment.GetLegalPlacements();
            int lines = environment.Step(candidates[bestIdx]);
            totalLines += lines;
            steps++;

            if (lines > 0)
                Debug.Log($"[NeuralNetPolicy] Step {steps}: cleared {lines} lines (total: {totalLines})");

            // Yield one frame so the board is visually updated
            yield return null;
        }

        Debug.Log($"[NeuralNetPolicy] Episode finished — {steps} steps, {totalLines} lines cleared, " +
                  $"done={environment.IsDone}");
    }
}
