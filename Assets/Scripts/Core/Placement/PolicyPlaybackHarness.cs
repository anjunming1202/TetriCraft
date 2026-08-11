using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interactive control surface for a NeuralNetPolicyDriver — same role as
/// HeadlessManualTestHarness, but for the value-net policy instead of manual input:
/// restart, pause/resume, step one placement at a time, and tune tick speed +
/// ticks-per-placement live.
///
/// The MODEL and the policy live on the referenced NeuralNetPolicyDriver (assign its model
/// in that component's Inspector). This harness owns NO model — it only reads input
/// (inspector-remappable keys) and renders the HUD, driving the policy via its public API.
///
/// Wiring: put this on the same GameObject as the NeuralNetPolicyDriver (it auto-finds it),
/// or assign `policy` explicitly.
/// </summary>
public class PolicyPlaybackHarness : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NeuralNetPolicyDriver policy;

    [Header("Transport keys")]
    [SerializeField] private KeyCode pauseResumeKey = KeyCode.Space;
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private KeyCode stepForwardKey = KeyCode.F;

    [Header("Tick-speed keys")]
    [SerializeField] private KeyCode speedUpKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode speedDownKey = KeyCode.DownArrow;
    [SerializeField] private float secondsPerTickStep = 0.05f;

    [Header("Ticks-per-placement keys")]
    [SerializeField] private KeyCode moreTicksKey = KeyCode.RightBracket;
    [SerializeField] private KeyCode fewerTicksKey = KeyCode.LeftBracket;

    [Header("HUD")]
    [SerializeField] private bool showHud = true;

    private void Start()
    {
        if (policy == null)
            policy = GetComponent<NeuralNetPolicyDriver>();
        if (policy == null)
            Debug.LogError("[PolicyPlayback] No NeuralNetPolicyDriver — assign `policy` or put this " +
                           "on the same GameObject as the driver.");
    }

    private void Update()
    {
        if (policy == null) return;

        if (Input.GetKeyDown(pauseResumeKey)) policy.TogglePause();
        if (Input.GetKeyDown(restartKey)) policy.RequestRestart();
        if (Input.GetKeyDown(stepForwardKey)) policy.RequestStep();
        if (Input.GetKeyDown(speedUpKey)) policy.AdjustSecondsPerTick(-secondsPerTickStep);   // faster
        if (Input.GetKeyDown(speedDownKey)) policy.AdjustSecondsPerTick(secondsPerTickStep);   // slower
        if (Input.GetKeyDown(moreTicksKey)) policy.AdjustTicksPerPlacement(1);
        if (Input.GetKeyDown(fewerTicksKey)) policy.AdjustTicksPerPlacement(-1);
    }

    private void OnGUI()
    {
        if (!showHud || policy == null) return;

        float spt = policy.SecondsPerTick;
        string speed = spt > 0f ? $"{1f / spt:0.#}/s ({spt:0.00}s)" : "max (instant)";

        var lines = new List<string>
        {
            $"Policy Playback — {(policy.IsPaused ? "PAUSED" : "PLAYING")}{(policy.IsEpisodeOver ? " (ended)" : "")}",
            $"Placements: {policy.PlacementCount} / {policy.MaxPlacements}    Lines: {policy.TotalLines}",
            $"Ticks/placement: {policy.TicksPerPlacement}    Tick speed: {speed}",
            $"[{pauseResumeKey}] pause/resume   [{restartKey}] restart   [{stepForwardKey}] step",
            $"[{speedUpKey}/{speedDownKey}] speed   [{moreTicksKey}/{fewerTicksKey}] ticks/placement",
        };

        const int lineHeight = 20;
        GUI.Box(new Rect(5, 5, 380, lineHeight * lines.Count + 10), "");
        for (int i = 0; i < lines.Count; i++)
            GUI.Label(new Rect(12, 8 + i * lineHeight, 370, lineHeight), lines[i]);
    }
}
