using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RandomTickDebugger : MonoBehaviour
{
    [SerializeField] private MapManager debuggedMap;

    [Header("Options")]
    [SerializeField] private bool showDebug = true;
    [SerializeField] private float holdDuration = 0.4f;

    [Header("Colors")]
    [SerializeField] private Color tickColor = new Color(1f, 1f, 0f, 0.7f);

    private struct TickFlash
    {
        public Vector3 worldPos;
        public float   startTime;
    }

    private readonly List<TickFlash> activeFlashes = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        if (debuggedMap?.RandomTickManager != null)
            debuggedMap.RandomTickManager.OnTickFired += HandleTickFired;
    }

    private void OnDestroy()
    {
        if (debuggedMap?.RandomTickManager != null)
            debuggedMap.RandomTickManager.OnTickFired -= HandleTickFired;
    }

    private void HandleTickFired(Vector3 worldPos)
    {
        if (!showDebug) return;
        activeFlashes.Add(new TickFlash { worldPos = worldPos, startTime = Time.realtimeSinceStartup });
    }

    // ── Update / Draw ─────────────────────────────────────────────────────────

    private void Update()
    {
        if (!showDebug) return;

        float now = Time.realtimeSinceStartup;
        activeFlashes.RemoveAll(f => now - f.startTime >= holdDuration);

#if UNITY_EDITOR
        if (activeFlashes.Count > 0)
            SceneView.RepaintAll();
#endif
    }

    private void OnDrawGizmos()
    {
        if (!showDebug || activeFlashes.Count == 0) return;

        float    size     = MapBoundaryData.unitSize;
        Vector3  cellSize = new Vector3(size, size, 0.01f);
        float    now      = Time.realtimeSinceStartup;

        foreach (var flash in activeFlashes)
        {
            float alpha = 1f - (now - flash.startTime) / holdDuration;
            Color c     = tickColor;
            c.a        *= alpha;
            Gizmos.color = c;
            Gizmos.DrawCube(flash.worldPos, cellSize);
        }
    }
}
