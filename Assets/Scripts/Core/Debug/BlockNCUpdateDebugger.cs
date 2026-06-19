using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BlockNCUpdateDebugger : MonoBehaviour
{
    [SerializeField] private MapManager debuggedMap;

    [Header("Options")]
    [SerializeField] private bool showDebug = true;
    [SerializeField] private float holdDuration = 0.6f;

    [Header("Colors")]
    [SerializeField] private Color sourceColor   = new Color(1f, 0.75f, 0f, 0.55f); // amber
    [SerializeField] private Color receiverColor = new Color(0f, 0.55f, 1f, 0.40f); // sky blue

    private struct Snapshot
    {
        public HashSet<Vector2Int> sources;
        public List<Vector2Int>    receivers;
        public float               startTime;
    }

    private readonly List<Snapshot> activeSnapshots = new();
    private float lastSeenSnapshotTime = -1f;

    private void Update()
    {
        if (!showDebug || debuggedMap == null) return;

        var ncManager = debuggedMap.BlockNCUpdateManager;
        if (ncManager == null) return;

        // Record new snapshot on each new trigger
        if (ncManager.DebugLastSnapshotTime != lastSeenSnapshotTime)
        {
            lastSeenSnapshotTime = ncManager.DebugLastSnapshotTime;
            activeSnapshots.Add(new Snapshot
            {
                sources   = new HashSet<Vector2Int>(ncManager.DebugLastSources),
                receivers = new List<Vector2Int>(ncManager.DebugLastReceiverPositions),
                startTime = Time.realtimeSinceStartup
            });
        }

        // Expire old snapshots
        float now = Time.realtimeSinceStartup;
        activeSnapshots.RemoveAll(s => now - s.startTime >= holdDuration);

#if UNITY_EDITOR
        if (activeSnapshots.Count > 0)
            SceneView.RepaintAll();
#endif
    }

    private void OnDrawGizmos()
    {
        if (!showDebug || debuggedMap == null || activeSnapshots.Count == 0) return;

        float   size     = MapBoundaryData.unitSize;
        Vector3 cellSize = new Vector3(size, size, 0.01f);
        var     boundary = BoundaryDataManager.GetBoundaryData(debuggedMap.PlayerID);
        float   now      = Time.realtimeSinceStartup;

        foreach (var snap in activeSnapshots)
        {
            float alpha = 1f - (now - snap.startTime) / holdDuration; // 1→0 linear fade

            Color sc = sourceColor;   sc.a *= alpha;
            Color rc = receiverColor; rc.a *= alpha;

            Gizmos.color = sc;
            foreach (var pos in snap.sources)
                Gizmos.DrawCube(boundary.GridToWorld(pos), cellSize);

            Gizmos.color = rc;
            foreach (var pos in snap.receivers)
                Gizmos.DrawCube(boundary.GridToWorld(pos), cellSize);
        }
    }
}
