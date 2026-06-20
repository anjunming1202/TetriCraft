using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Visualizes ScheduledTickManager state in the editor:
/// • GameTick counter overlay in both Scene view and Game view.
/// • Colored dot + "T-N" remaining-ticks label above each pending scheduled entry.
///   Color fades orange → red as the entry approaches its fire tick.
/// • Cyan cube flash at the flame position when a scheduled tick fires.
/// </summary>
public class ScheduledTickDebugger : MonoBehaviour
{
    [SerializeField] private MapManager debuggedMap;

    [Header("Options")]
    [SerializeField] private bool showDebug   = true;
    [SerializeField] private float flashDuration = 0.35f;

    [Header("Colors")]
    [SerializeField] private Color pendingFarColor  = new Color(1f, 0.55f, 0f, 0.85f);
    [SerializeField] private Color pendingNearColor = new Color(1f, 0.1f,  0f, 0.85f);
    [SerializeField] private Color firedColor       = new Color(0f, 1f,    1f, 0.75f);

    // ── Pending entries: (targetTick, worldPos) ───────────────────────────────
    // Using a list instead of SortedList because we need to remove specific entries
    // when they fire (matched by targetTick + position).
    private readonly List<(uint targetTick, Vector3 pos)> pendingEntries = new();

    // ── Fired flashes ─────────────────────────────────────────────────────────
    private struct TickFlash
    {
        public Vector3 worldPos;
        public float   startTime;
    }

    private readonly List<TickFlash> activeFlashes = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        var stm = debuggedMap?.ScheduledTickManager;
        if (stm == null) return;
        stm.OnEntryScheduled += HandleEntryScheduled;
        stm.OnEntryFired     += HandleEntryFired;
    }

    private void OnDestroy()
    {
        var stm = debuggedMap?.ScheduledTickManager;
        if (stm == null) return;
        stm.OnEntryScheduled -= HandleEntryScheduled;
        stm.OnEntryFired     -= HandleEntryFired;
    }

    private void HandleEntryScheduled(uint targetTick, Vector3 pos)
    {
        if (!showDebug) return;
        pendingEntries.Add((targetTick, pos));
    }

    private void HandleEntryFired(uint targetTick, Vector3 pos)
    {
        if (!showDebug) return;

        // Remove the first matching pending entry.
        for (int i = 0; i < pendingEntries.Count; i++)
        {
            if (pendingEntries[i].targetTick == targetTick && pendingEntries[i].pos == pos)
            {
                pendingEntries.RemoveAt(i);
                break;
            }
        }

        activeFlashes.Add(new TickFlash { worldPos = pos, startTime = Time.realtimeSinceStartup });
    }

    // ── Update / Draw ─────────────────────────────────────────────────────────

    private void Update()
    {
        if (!showDebug) return;

        float now = Time.realtimeSinceStartup;
        activeFlashes.RemoveAll(f => now - f.startTime >= flashDuration);

#if UNITY_EDITOR
        if (activeFlashes.Count > 0 || pendingEntries.Count > 0)
            SceneView.RepaintAll();
#endif
    }

    /// <summary>Game view overlay: GameTick counter.</summary>
    private void OnGUI()
    {
        if (!showDebug) return;
        GUI.Box(new Rect(8, 8, 200, 24), $"  GameTick: {TickManager.GameTick}");
    }

    private void OnDrawGizmos()
    {
        if (!showDebug) return;

#if UNITY_EDITOR
        // ── Scene view: GameTick counter ──────────────────────────────────────
        Handles.BeginGUI();
        GUI.Box(new Rect(8, 8, 200, 24), $"  GameTick: {TickManager.GameTick}");
        Handles.EndGUI();

        // ── Pending entries ───────────────────────────────────────────────────
        if (pendingEntries.Count > 0)
        {
            var labelStyle = new GUIStyle { fontSize = 9, fontStyle = FontStyle.Bold };

            foreach (var (targetTick, pos) in pendingEntries)
            {
                long remaining = (long)targetTick - (long)TickManager.GameTick;
                if (remaining < 0) remaining = 0;

                float t   = 1f - Mathf.Clamp01((float)remaining / 35f); // 0 = far, 1 = imminent
                Color dot = Color.Lerp(pendingFarColor, pendingNearColor, t);

                Gizmos.color = dot;
                Gizmos.DrawSphere(pos, 0.18f);

                labelStyle.normal.textColor = Color.Lerp(Color.yellow, Color.red, t);
                Handles.Label(pos + Vector3.up * 0.35f, $"T-{remaining}", labelStyle);
            }
        }

        // ── Fired flashes ─────────────────────────────────────────────────────
        float now    = Time.realtimeSinceStartup;
        float size   = MapBoundaryData.unitSize;
        var cubeSize = new Vector3(size, size, 0.01f);

        foreach (var flash in activeFlashes)
        {
            float alpha = 1f - (now - flash.startTime) / flashDuration;
            Color c     = firedColor;
            c.a        *= alpha;
            Gizmos.color = c;
            Gizmos.DrawCube(flash.worldPos, cubeSize);
        }
#endif
    }
}
