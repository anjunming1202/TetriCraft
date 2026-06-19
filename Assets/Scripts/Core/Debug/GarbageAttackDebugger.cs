using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Scene-view Gizmos overlay for the battle garbage attack system.
/// Attach to any active GameObject in the battle scene.
///
/// For each player it draws:
///   - Stacked coloured rows at the bottom of their board representing pending
///     garbage waves (bottom wave = first to arrive; colours cycle through the
///     wave palette so individual waves are distinguishable).
///   - A text label above the stack showing total pending line count.
///
/// Only renders at runtime (requires boundaryData, which is built on Initialise).
/// </summary>
public class GarbageAttackDebugger : MonoBehaviour
{
    [SerializeField] private BattleTetrisManager tetrisP1;
    [SerializeField] private BattleTetrisManager tetrisP2;
    [SerializeField] private PlayerGameManager   gameManagerP1;
    [SerializeField] private PlayerGameManager   gameManagerP2;

    [Header("Display")]
    [SerializeField] private bool showDebug = true;

    [Tooltip("Alpha of the filled wave rectangles.")]
    [SerializeField, Range(0f, 1f)] private float fillAlpha = 0.35f;

    [Tooltip("Colours cycled through per wave (index 0 = first/oldest wave).")]
    [SerializeField] private Color[] waveColors = new Color[]
    {
        new Color(1.00f, 0.25f, 0.20f), // red    – oldest wave
        new Color(1.00f, 0.55f, 0.10f), // orange
        new Color(1.00f, 0.90f, 0.15f), // yellow
        new Color(0.30f, 0.80f, 0.25f), // green
        new Color(0.20f, 0.60f, 1.00f), // blue   – newest wave
    };

#if UNITY_EDITOR
    private readonly List<int> _wavesP1 = new();
    private readonly List<int> _wavesP2 = new();

    private void OnDrawGizmos()
    {
        if (!showDebug) return;

        DrawPlayerGarbage(tetrisP1, gameManagerP1, _wavesP1, "P1");
        DrawPlayerGarbage(tetrisP2, gameManagerP2, _wavesP2, "P2");
    }

    private void DrawPlayerGarbage(BattleTetrisManager tetris, PlayerGameManager gm,
                                   List<int> waveBuf, string label)
    {
        if (tetris == null || gm == null) return;

        MapBoundaryData bd = gm.boundaryData;
        if (bd == null) return; // only available at runtime after Initialise()

        tetris.DebugGetGarbageWaves(waveBuf);

        int boardWidth = bd.width;
        float unit     = MapBoundaryData.unitSize;
        Vector3 origin = bd.origin;

        // Draw stacked rows from the bottom of the board upward, one wave at a time.
        int rowCursor = 0;
        for (int wi = 0; wi < waveBuf.Count; wi++)
        {
            int waveLines = waveBuf[wi];
            if (waveLines <= 0) continue;

            Color waveColor = waveColors[wi % waveColors.Length];

            for (int row = 0; row < waveLines; row++)
            {
                int gridRow = rowCursor + row;
                // Draw each cell of this row individually so the fill matches the grid.
                for (int x = 0; x < boardWidth; x++)
                {
                    Vector3 centre = origin + new Vector3((x + 0.5f) * unit, (gridRow + 0.5f) * unit, 0f);
                    Vector3 size   = new Vector3(unit, unit, 0.01f);

                    Color fill  = waveColor; fill.a  = fillAlpha;
                    Color wire  = waveColor; wire.a  = Mathf.Min(fill.a + 0.35f, 1f);

                    Gizmos.color = fill;
                    Gizmos.DrawCube(centre, size);
                    Gizmos.color = wire;
                    Gizmos.DrawWireCube(centre, size);
                }
            }
            rowCursor += waveLines;
        }

        // Label above the stack (or above the board origin if nothing is queued).
        int total = 0;
        foreach (int l in waveBuf) total += l;

        float labelY  = origin.y + Mathf.Max(rowCursor + 0.1f, 0.5f) * unit;
        float labelX  = origin.x + boardWidth * unit * 0.5f;
        Vector3 labelPos = new Vector3(labelX, labelY, 0f);

        string text = total > 0
            ? $"{label} incoming: {total} line{(total == 1 ? "" : "s")} ({waveBuf.Count} wave{(waveBuf.Count == 1 ? "" : "s")})"
            : $"{label} incoming: none";

        Handles.Label(labelPos, text);
    }
#endif
}
