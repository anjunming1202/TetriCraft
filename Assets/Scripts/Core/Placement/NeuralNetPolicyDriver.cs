using System.Collections;

using UnityEngine;

/// <summary>
/// Drives RolloutEnvironment with the value network: each placement enumerates the legal
/// after-states, evaluates them in one batched forward pass, and commits argmax V(s').
///
/// The model is configured HERE (`valueNetModel`) — this is the single owner of the policy.
/// The play loop is controllable (pause / single-step / restart / per-placement tick pacing)
/// so an external control surface (PolicyPlaybackHarness) can drive it via the public API
/// below WITHOUT owning the model. Runs standalone too: it auto-plays on Start with instant
/// pacing, preserving its original drop-in behaviour.
/// </summary>
public class NeuralNetPolicyDriver : MonoBehaviour
{
    [Header("Policy")]
    [SerializeField] private RolloutEnvironment environment;
    [SerializeField] private Unity.InferenceEngine.ModelAsset valueNetModel;
    [Tooltip("ON: use `seed` (reproducible). OFF: fresh random seed each episode/restart for a varied " +
             "demo (the chosen seed is logged so you can reproduce a good run).")]
    [SerializeField] private bool useFixedSeed = true;
    [SerializeField] private int seed = 42;
    [SerializeField] private int maxSteps = 5000;

    [Header("Playback (live-adjustable; also drivable via PolicyPlaybackHarness)")]
    [SerializeField] private bool startPaused = false;
    [Tooltip("Game ticks simulated after each placement (1 is fine for inert blocks; raise for fluid/TNT).")]
    [SerializeField, Range(1, 60)] private int ticksPerPlacement = 1;
    [Tooltip("Real seconds between ticks. 0 = instant (one placement per frame). Higher = slower.")]
    [SerializeField, Range(0f, 2f)] private float secondsPerTick = 0f;

    // Control state — set via the public API, typically by PolicyPlaybackHarness.
    private bool isPaused;
    private bool stepRequested;
    private bool restartRequested;

    // Live stats.
    private int placementCount;
    private int totalLines;
    private int lastPlacementLines;
    private bool episodeOver;

    // Inference buffers (sized on first episode init).
    private byte[][] afterStates;
    private int[] linesCleared;
    private float[] values;
    private const int MaxCandidates = 80;
    private int boardW, boardH;

    // --- Public control API (used by PolicyPlaybackHarness) ------------------------- //
    public bool IsPaused => isPaused;
    public bool IsEpisodeOver => episodeOver;
    public int PlacementCount => placementCount;
    public int MaxPlacements => maxSteps;
    public int TotalLines => totalLines;
    public int LastPlacementLines => lastPlacementLines;
    public int TicksPerPlacement => ticksPerPlacement;
    public float SecondsPerTick => secondsPerTick;

    public void TogglePause() => isPaused = !isPaused;
    public void SetPaused(bool paused) => isPaused = paused;
    public void RequestStep() => stepRequested = true;      // advance one placement, then pause
    public void RequestRestart() => restartRequested = true;
    public void AdjustTicksPerPlacement(int delta) => ticksPerPlacement = Mathf.Clamp(ticksPerPlacement + delta, 1, 60);
    public void AdjustSecondsPerTick(float delta) => secondsPerTick = Mathf.Clamp(secondsPerTick + delta, 0f, 2f);

    private void Start()
    {
        if (environment == null || valueNetModel == null)
        {
            Debug.LogError("[NeuralNetPolicy] Assign `environment` and `valueNetModel` in the Inspector.");
            enabled = false;
            return;
        }
        ValueNetInference.LoadModel(valueNetModel);
        isPaused = startPaused;
        StartCoroutine(PlayLoop());
    }

    private void OnDestroy() => ValueNetInference.Dispose();

    private void EnsureBuffers(int width, int height)
    {
        if (afterStates != null) return;
        int boardSize = width * height;
        afterStates = new byte[MaxCandidates][];
        for (int i = 0; i < MaxCandidates; i++)
            afterStates[i] = new byte[boardSize];
        linesCleared = new int[MaxCandidates];
        values = new float[MaxCandidates];
    }

    private void InitEpisode()
    {
        environment.Reset(SeedOption.Resolve(useFixedSeed, seed, "PolicyDriver"));
        boardW = environment.BoardWidth;
        boardH = environment.BoardHeight;
        EnsureBuffers(boardW, boardH);
        placementCount = 0;
        totalLines = 0;
        lastPlacementLines = 0;
        episodeOver = false;
    }

    private IEnumerator PlayLoop()
    {
        while (true)
        {
            InitEpisode();

            while (!episodeOver)
            {
                // Idle while paused, unless a single step or restart was requested.
                while (isPaused && !stepRequested && !restartRequested)
                    yield return null;

                if (restartRequested) { restartRequested = false; break; }

                if (environment.IsDone || placementCount >= maxSteps)
                {
                    episodeOver = true;
                    break;
                }

                bool single = stepRequested;
                stepRequested = false;

                int n = environment.QueryAfterStates(afterStates, linesCleared);
                if (n == 0) { episodeOver = true; break; }   // topout: no legal placement

                ValueNetInference.EvaluateBatch(afterStates, values, n, boardW, boardH);
                int best = 0;
                for (int i = 1; i < n; i++)
                    if (values[i] > values[best]) best = i;
                var candidates = environment.GetLegalPlacements();

                // Commit, then pace this placement's ticks in real time.
                environment.CommitPlacement(candidates[best]);
                int ticks = Mathf.Max(1, ticksPerPlacement);
                for (int t = 0; t < ticks; t++)
                {
                    environment.AdvanceOneTick();
                    if (secondsPerTick > 0f) yield return new WaitForSeconds(secondsPerTick);
                    else yield return null;
                    if (restartRequested) break;   // stay responsive to restart mid-placement
                }

                lastPlacementLines = environment.LinesClearedThisTurn;
                totalLines += lastPlacementLines;
                placementCount++;
                if (lastPlacementLines > 0)
                    Debug.Log($"[NeuralNetPolicy] Placement {placementCount}: cleared {lastPlacementLines} " +
                              $"(total {totalLines})");

                if (single) isPaused = true;   // "step forward" = advance one, then pause
            }

            if (episodeOver)
                Debug.Log($"[NeuralNetPolicy] Episode ended — {placementCount} placements, " +
                          $"{totalLines} lines, done={environment.IsDone}. Restart to replay.");

            // Hold on the final board until a restart is requested.
            while (!restartRequested)
                yield return null;
            restartRequested = false;
        }
    }
}
