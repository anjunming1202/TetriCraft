using Cysharp.Threading.Tasks;

using UnityEngine;

/// <summary>
/// Runs a trained value-net policy on the ACTUAL gameplay board. Unlike NeuralNetPolicyDriver
/// (which commits each placement instantly via RolloutEnvironment.Step / CommitPlacementInstant),
/// this executes the chosen placement through the real tetromino controls: it decodes the target
/// rotation + column into rotate/shift/drop ops (PlacementDecoder) and ApplyOp's them one at a time,
/// so you watch the piece actually rotate, shift, and hard-drop (with the real lock delay) on the
/// live board — the true-gameplay counterpart of RandomPlacementDemoDriver.
///
/// Same structure as RandomPlacementDemoDriver; only the selection differs: argmax V(after-state)
/// (or argmax[lines + V] if rewardAware), using the same batched Sentis inference NeuralNetPolicyDriver
/// uses. Lives on its OWN GameObject — assign the target PlacementTetrisManager and the model.
/// </summary>
[DefaultExecutionOrder(-100)]   // run early so the seed lands before the board draws its first pieces
public class NeuralNetGameplayDriver : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The PlacementTetrisManager driving the board (typically on another GameObject). " +
             "Auto-found on this GameObject if left empty.")]
    [SerializeField] private PlacementTetrisManager tetrisManager;

    [Header("Policy")]
    [SerializeField] private Unity.InferenceEngine.ModelAsset valueNetModel;
    [Tooltip("Selection rule.\n" +
             "  ON  (reward-aware):  i* = argmaxᵢ [ rᵢ + γ · V(s'ᵢ) ]   — favours immediate clears\n" +
             "  OFF (V-only):        i* = argmaxᵢ V(s'ᵢ)                 — matches the training default\n" +
             "where s'ᵢ = after-state of placement i, rᵢ = lines it clears, γ = discount (1.0).")]
    [SerializeField] private bool rewardAware = false;

    [Header("Playback")]
    [Tooltip("Real seconds between each rotate/shift op, so the moves are watchable.")]
    [SerializeField] private float stepDelaySeconds = 0.1f;

    [Header("Episode")]
    [Tooltip("ON: use `seed` (reproducible episode). OFF: draw a fresh random seed each run for a " +
             "varied demo (the chosen seed is logged so you can reproduce a good run).")]
    [SerializeField] private bool useFixedSeed = true;
    [Tooltip("Seeds the game's piece/material RNG (UnityEngine.Random) at startup. The policy is " +
             "deterministic (argmax), so the seed alone determines the whole episode.")]
    [SerializeField] private int seed = 42;

    private bool isPlaying;

    // Pre-allocated inference buffers (sized on first turn).
    private byte[][] afterStates;
    private int[] linesCleared;
    private float[] values;
    private byte[] baseGrid;
    private const int MaxCandidates = 80;

    private void Awake()
    {
        // Seed the game's piece/material RNG before the map draws its first pieces. The
        // [DefaultExecutionOrder(-100)] above makes this Awake run early so the seed lands first.
        UnityEngine.Random.InitState(SeedOption.Resolve(useFixedSeed, seed, "PolicyGameplay"));

        if (tetrisManager == null)
            tetrisManager = GetComponent<PlacementTetrisManager>();   // fallback: same-GameObject usage
        if (tetrisManager == null || valueNetModel == null)
        {
            Debug.LogError("[NeuralNetGameplay] Assign `tetrisManager` and `valueNetModel` in the Inspector.");
            enabled = false;
            return;
        }
        // Load in Awake so the model is ready before the first OnStartedTurn (which can fire from
        // another component's Start when the map starts).
        ValueNetInference.LoadModel(valueNetModel);
    }

    private void OnDestroy()
    {
        if (valueNetModel != null)
            ValueNetInference.Dispose();
    }

    private void OnEnable()
    {
        if (tetrisManager != null)
            tetrisManager.OnStartedTurn += HandleStartedTurn;
    }

    private void OnDisable()
    {
        if (tetrisManager != null)
            tetrisManager.OnStartedTurn -= HandleStartedTurn;
    }

    private void HandleStartedTurn()
    {
        if (isPlaying) return;
        PlayPolicyPlacementAsync().Forget();
    }

    private void EnsureBuffers(int width, int height)
    {
        if (afterStates != null) return;
        int size = width * height;
        afterStates = new byte[MaxCandidates][];
        for (int i = 0; i < MaxCandidates; i++)
            afterStates[i] = new byte[size];
        linesCleared = new int[MaxCandidates];
        values = new float[MaxCandidates];
        baseGrid = new byte[size];
    }

    private async UniTaskVoid PlayPolicyPlacementAsync()
    {
        isPlaying = true;

        var candidates = tetrisManager.GetLegalPlacements();
        if (candidates.Count == 0)
        {
            Debug.LogWarning("[NeuralNetGameplay] No legal placements for the current piece.");
            isPlaying = false;
            return;
        }

        int w = tetrisManager.BoundaryWidth;
        int h = tetrisManager.BoundaryHeight;
        EnsureBuffers(w, h);

        // Build after-states for every candidate — same computation as RolloutEnvironment.QueryAfterStates.
        int n = Mathf.Min(candidates.Count, MaxCandidates);
        tetrisManager.GetBoardOccupancy(baseGrid);
        for (int i = 0; i < n; i++)
            linesCleared[i] = tetrisManager.ComputeAfterState(baseGrid, candidates[i], afterStates[i]);

        // Batched V(s') and argmax (reward-aware optionally adds the immediate line-clear reward).
        ValueNetInference.EvaluateBatch(afterStates, values, n, w, h);
        int best = 0;
        float bestScore = Score(0);
        for (int i = 1; i < n; i++)
        {
            float s = Score(i);
            if (s > bestScore) { bestScore = s; best = i; }
        }
        PlacementCandidate chosen = candidates[best];

        // Execute through the real controls: rotate first, then shift (re-read the column AFTER
        // rotation, since wall kicks can move x), then a real hard drop with lock delay.
        foreach (var op in PlacementDecoder.DecodeRotation(tetrisManager.FallingRotation, chosen.Rotation))
        {
            tetrisManager.ApplyOp(op);
            await UniTask.Delay(System.TimeSpan.FromSeconds(stepDelaySeconds));
        }
        foreach (var op in PlacementDecoder.DecodeShift(tetrisManager.FallingColumn, chosen.Column))
        {
            tetrisManager.ApplyOp(op);
            await UniTask.Delay(System.TimeSpan.FromSeconds(stepDelaySeconds));
        }
        tetrisManager.ApplyOp(PlacementDecoder.PlacementOp.Drop);

        isPlaying = false;
    }

    private float Score(int i) => rewardAware ? linesCleared[i] + values[i] : values[i];
}
