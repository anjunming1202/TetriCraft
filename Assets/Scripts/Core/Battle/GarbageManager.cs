using System.Collections.Generic;
using UnityEngine;

public class GarbageManager : MonoBehaviour
{
    private MapManager map;

    private GarbageConfig config;
    private int boundaryWidth;

    private struct GarbageWave
    {
        public int  lines;
        public uint sourceClears;
        public uint sourceCombo;
    }
    private readonly List<GarbageWave> _pendingWaves = new();

    public void Initialise(int width, GarbageConfig cfg, MapManager targetMap)
    {
        boundaryWidth = width;
        config = cfg;
        map = targetMap;
        _pendingWaves.Clear();
        Debug.Log($"[GarbageManager] Initialised — width={width}, config={(cfg != null ? cfg.name : "NULL")}, map={(targetMap != null ? targetMap.name : "NULL")}");
    }

    public void Reset() => _pendingWaves.Clear();

#if UNITY_EDITOR
    public int DebugTotalPending
    {
        get { int t = 0; foreach (var w in _pendingWaves) t += w.lines; return t; }
    }

    // Fills result with one entry per pending wave (line count only).
    public void DebugGetWaveLines(System.Collections.Generic.List<int> result)
    {
        result.Clear();
        foreach (var w in _pendingWaves) result.Add(w.lines);
    }
#endif

    /// <summary>
    /// Cancels up to <paramref name="attack"/> lines from the front of the pending incoming garbage.
    /// Returns the overflow — lines remaining after all pending is cancelled.
    /// overflow == 0 means the attack was fully absorbed; overflow > 0 means a net counter-attack.
    /// </summary>
    public int CancelIncoming(int attack)
    {
        int totalPending = 0;
        foreach (var w in _pendingWaves) totalPending += w.lines;
        if (totalPending <= 0) return attack;

        int remaining = attack;
        while (remaining > 0 && _pendingWaves.Count > 0)
        {
            var wave = _pendingWaves[0];
            int cancel = Mathf.Min(remaining, wave.lines);
            wave.lines -= cancel;
            remaining  -= cancel;
            if (wave.lines <= 0)
                _pendingWaves.RemoveAt(0);
            else
                _pendingWaves[0] = wave;
        }
        Debug.Log($"[GarbageManager] Cancelled {attack - remaining} incoming line(s), overflow={remaining}, remaining pending={totalPending - (attack - remaining)}");
        return remaining;
    }

    public void Queue(int lines, uint sourceClears = 0, uint sourceCombo = 0)
    {
        if (lines <= 0) return;
        _pendingWaves.Add(new GarbageWave { lines = lines, sourceClears = sourceClears, sourceCombo = sourceCombo });
        Debug.Log($"[GarbageManager] Queued {lines} line(s), waves={_pendingWaves.Count}");
    }

    /// <summary>Call this in TetrisManager.OnNextTurn() before SpawnTetromino.</summary>
    public void InsertPending()
    {
        if (config == null)
        {
            Debug.LogWarning("[GarbageManager] InsertPending skipped — config is null");
            return;
        }
        if (_pendingWaves.Count == 0) return;

        // Merge all pending waves into one insert, picking the dominant attack context
        int  totalLines = 0;
        uint maxClears  = 0;
        uint maxCombo   = 0;
        foreach (var w in _pendingWaves)
        {
            totalLines += w.lines;
            if (w.sourceClears > maxClears) { maxClears = w.sourceClears; maxCombo = w.sourceCombo; }
        }
        _pendingWaves.Clear();

        Debug.Log($"[GarbageManager] Inserting {totalLines} garbage row(s), boundaryWidth={boundaryWidth}");

        var ctx = new GarbageInsertContext
        {
            totalRows    = totalLines,
            boardWidth   = boundaryWidth,
            sourceClears = maxClears,
            sourceCombo  = maxCombo,
        };

        BlockID?[,] layout = config.GetGarbageLayout(ctx);

        ShiftRowsUp(totalLines);
        map.FluidSystem.ShiftElementsUp(totalLines);
        SpawnGarbageRows(layout);
        AnimateGarbageRise(totalLines);
        Debug.Log($"[GarbageManager] Insert complete");
    }

    private void ShiftRowsUp(int count)
    {
        int shifted = 0, destroyed = 0;
        for (int y = map.GridHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < boundaryWidth; x++)
            {
                Block block = map.GetBlock(x, y);
                if (map.CheckEmpty(x, y) || !block.isLocked) continue;

                int targetY = y + count;
                if (targetY >= map.GridHeight)
                {
                    map.RequestDestroyBlock(block);
                    destroyed++;
                }
                else
                {
                    map.RequestMoveBlock(block, x, targetY);
                    shifted++;
                }
            }
            map.ImmediatelyProcessGridPendingUpdates();
        }
        Debug.Log($"[GarbageManager] ShiftRowsUp: shifted={shifted}, destroyed={destroyed}");
    }

    /// <summary>
    /// After garbage rows have been committed to the grid, sets each block's visual start
    /// position to <paramref name="count"/> rows below its target and triggers an animated
    /// move upward using the existing BlockAnimator / OnAnimatedMove pipeline.
    /// Grid data is already correct at this point — only the visual transform is changed.
    /// </summary>
    private void AnimateGarbageRise(int count)
    {
        MapBoundaryData bd = BoundaryDataManager.GetBoundaryData(map.PlayerID);
        for (int row = 0; row < count; row++)
        {
            for (int x = 0; x < boundaryWidth; x++)
            {
                Block block = map.GetBlock(x, row);
                if (block == null || block.IsDummy) continue;

                // Place the visual start point count rows below the target position
                block.transform.position = bd.MapToWorld(
                    new Vector2(block.CentrePosition.x, block.CentrePosition.y - count));

                // Re-trigger animated move — BlockAnimator interpolates from current
                // transform.position (below) to block.GetWorldPosition() (target row)
                block.SetGridPosition(block.GridPosition.x, block.GridPosition.y, animation: true);
            }
        }
    }

    private void SpawnGarbageRows(BlockID?[,] layout)
    {
        int rowCount = layout.GetLength(0);
        int width    = layout.GetLength(1);
        for (int row = 0; row < rowCount; row++)
        {
            for (int x = 0; x < width; x++)
            {
                if (layout[row, x] is not BlockID blockID) continue;
                Block garb = BlockSpawner.NewBlock(blockID);
                map.RequestSpawnBlock(garb, x, row);
            }
            map.ImmediatelyProcessGridPendingUpdates();
            Debug.Log($"[GarbageManager] Spawned garbage row {row}");
        }
    }
}
