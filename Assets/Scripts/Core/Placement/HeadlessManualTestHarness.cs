using System.Collections.Generic;
using UnityEngine;

// Low-level verification tool for the headless/training seam itself — not for watching a policy.
// Drives RolloutEnvironment directly (Reset/Step/GetLegalPlacements), the same API a training loop
// will eventually call over IPC. Controls mirror real Tetris input (move/rotate/drop) rather than
// cycling an abstract candidate list, plus manual/automatic tick controls for checking tick-gated
// dynamics ahead of later stages.
public class HeadlessManualTestHarness : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RolloutEnvironment environment;
    [SerializeField] private PlacementTetrisManager tetrisManager;
    [Tooltip("Optional — only used to display score/lines-cleared on the HUD.")]
    [SerializeField] private ScoreManager scoreManager;

    [Header("Episode")]
    [SerializeField] private int seed = 12345;

    [Header("Placement controls")]
    [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
    [SerializeField] private KeyCode moveRightKey = KeyCode.D;
    [SerializeField] private KeyCode rotateCCWKey = KeyCode.Q;
    [SerializeField] private KeyCode rotateCWKey = KeyCode.E;
    [SerializeField] private KeyCode commitKey = KeyCode.Space;
    [SerializeField] private bool logSelectedCandidate = true;

    [Header("Tick controls")]
    [Tooltip("Ticks per second while unpaused. 0 = uncapped (one tick per rendered frame).")]
    [SerializeField] private float ticksPerSecond = 0f;
    [SerializeField] private KeyCode togglePauseKey = KeyCode.P;
    [SerializeField] private KeyCode increaseTickRateKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode decreaseTickRateKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode forceSingleTickKey = KeyCode.RightArrow;
    [Tooltip("How many ticks a committed step advances while paused. In training this is the only " +
             "tick advancement a step ever does — the unpaused auto-tick mode here exists purely to " +
             "eyeball tick-gated dynamics (fluid/redstone, later stages) run correctly.")]
    [SerializeField] private int ticksPerStep = 1;
    [SerializeField] private KeyCode increaseTicksPerStepKey = KeyCode.Equals;
    [SerializeField] private KeyCode decreaseTicksPerStepKey = KeyCode.Minus;
    [SerializeField] private bool isPaused = true;

    [Header("Board control")]
    [SerializeField] private KeyCode restartKey = KeyCode.R;

    private int targetRotation;
    private int targetColumn;
    private int totalSteps;
    private float tickAccumulator;

    private void Start()
    {
        if (scoreManager != null)
            scoreManager.SubscribeMap(tetrisManager);

        environment.Reset(seed);
        scoreManager?.Reset();
    }

    private void OnEnable() => tetrisManager.OnStartedTurn += HandleStartedTurn;
    private void OnDisable() => tetrisManager.OnStartedTurn -= HandleStartedTurn;

    private void HandleStartedTurn()
    {
        targetRotation = tetrisManager.FallingRotation;
        targetColumn = tetrisManager.FallingColumn;

        PreviewTarget();
    }

    private void Update()
    {
        AdvanceAutomaticTicks();

        if (Input.GetKeyDown(restartKey))
        {
            environment.Reset(seed);
            scoreManager?.Reset();
            totalSteps = 0;
            return;
        }

        if (Input.GetKeyDown(togglePauseKey))
            isPaused = !isPaused;

        if (Input.GetKeyDown(increaseTickRateKey))
            ticksPerSecond = Mathf.Max(0f, ticksPerSecond + 1f);
        if (Input.GetKeyDown(decreaseTickRateKey))
            ticksPerSecond = Mathf.Max(0f, ticksPerSecond - 1f);

        if (Input.GetKeyDown(increaseTicksPerStepKey))
            ticksPerStep++;
        if (Input.GetKeyDown(decreaseTicksPerStepKey))
            ticksPerStep = Mathf.Max(1, ticksPerStep - 1);

        if (Input.GetKeyDown(forceSingleTickKey))
        {
            isPaused = true;
            TickManager.AdvanceTicks(1);
            tetrisManager.OnUpdate(); // pump tick-gated systems (fluid etc.) — AdvanceTicks alone only moves the clock
            PreviewTarget(); // reassert our ghost preview over OnUpdate()'s automatic one, if enabled
        }

        if (environment.IsDone)
            return;

        var candidates = environment.GetLegalPlacements();
        if (candidates.Count == 0)
            return;

        if (Input.GetKeyDown(moveLeftKey))
            TryMoveTarget(candidates, rotationDelta: 0, columnDelta: -1);
        if (Input.GetKeyDown(moveRightKey))
            TryMoveTarget(candidates, rotationDelta: 0, columnDelta: 1);
        if (Input.GetKeyDown(rotateCCWKey))
            TryMoveTarget(candidates, rotationDelta: -1, columnDelta: 0);
        if (Input.GetKeyDown(rotateCWKey))
            TryMoveTarget(candidates, rotationDelta: 1, columnDelta: 0);

        if (Input.GetKeyDown(commitKey))
            TryCommit(candidates);
    }

    private void AdvanceAutomaticTicks()
    {
        if (isPaused)
            return;

        if (ticksPerSecond <= 0f)
        {
            TickManager.AdvanceTicks(1); // uncapped: one tick per rendered frame
            tetrisManager.OnUpdate();
            PreviewTarget(); // OnUpdate()'s automatic ghost update (if enabled) just reset the ghost to the real falling piece's spawn state — reassert ours
            return;
        }

        tickAccumulator += Time.deltaTime;
        float interval = 1f / ticksPerSecond;
        while (tickAccumulator >= interval)
        {
            TickManager.AdvanceTicks(1);
            tetrisManager.OnUpdate();
            tickAccumulator -= interval;
        }
        PreviewTarget();
    }

    private void TryMoveTarget(IReadOnlyList<PlacementCandidate> candidates, int rotationDelta, int columnDelta)
    {
        int newRotation = ((targetRotation + rotationDelta) % 4 + 4) % 4;
        int newColumn = targetColumn + columnDelta;

        if (!TryFindCandidate(candidates, newRotation, newColumn, out var candidate))
            return; // blocked (wall/collision) — mirrors real Tetris movement, just don't move

        targetRotation = newRotation;
        targetColumn = newColumn;
        tetrisManager.PreviewCandidate(candidate);

        if (logSelectedCandidate)
            Debug.Log($"[HeadlessManualTestHarness] Target rotation={targetRotation}, column={targetColumn}, landingY={candidate.LandingY}");
    }

    private void TryCommit(IReadOnlyList<PlacementCandidate> candidates)
    {
        if (!TryFindCandidate(candidates, targetRotation, targetColumn, out var candidate))
            return;

        environment.Step(candidate);
        totalSteps++;

        if (isPaused)
            TickManager.AdvanceTicks(ticksPerStep);
    }

    private void PreviewTarget()
    {
        var candidates = environment.GetLegalPlacements();
        if (TryFindCandidate(candidates, targetRotation, targetColumn, out var candidate))
            tetrisManager.PreviewCandidate(candidate);
    }

    private static bool TryFindCandidate(IReadOnlyList<PlacementCandidate> candidates, int rotation, int column, out PlacementCandidate candidate)
    {
        foreach (var c in candidates)
        {
            if (c.Rotation == rotation && c.Column == column)
            {
                candidate = c;
                return true;
            }
        }
        candidate = default;
        return false;
    }

    private void OnGUI()
    {
        string tickRateLabel = ticksPerSecond <= 0f ? "uncapped (1/frame)" : $"{ticksPerSecond}/s";

        var lines = new List<string>
        {
            $"Total ticks: {TickManager.GameTick}",
            $"Ticks per step (paused): {ticksPerStep}",
            $"Auto tick rate: {tickRateLabel}",
            $"Paused: {isPaused}",
            $"Total steps: {totalSteps}",
            $"Next: {NextPiecesSummary()}",
        };

        if (scoreManager != null)
        {
            lines.Add($"Score: {scoreManager.Score}");
            lines.Add($"Lines cleared: {scoreManager.TotalLinesCleared}");
        }

        const int lineHeight = 20;
        GUI.Box(new Rect(5, 5, 340, lineHeight * lines.Count + 10), "");
        for (int i = 0; i < lines.Count; i++)
            GUI.Label(new Rect(12, 8 + i * lineHeight, 330, lineHeight), lines[i]);
    }

    private string NextPiecesSummary()
    {
        var parts = new List<string>();
        foreach (var next in tetrisManager.nextTetrominos)
        {
            string blockLabel = next.blocks[0] != null ? next.blocks[0].ID.ToString() : "?";
            parts.Add($"{next.type}({blockLabel})");
        }
        return string.Join(", ", parts);
    }
}
