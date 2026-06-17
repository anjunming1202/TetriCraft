using UnityEngine;

public class GarbageManager : MonoBehaviour
{
    private MapManager map;

    private GarbageConfig config;
    private int pending;
    private int boundaryWidth;

    public void Initialise(int width, GarbageConfig cfg, MapManager targetMap)
    {
        boundaryWidth = width;
        config = cfg;
        map = targetMap;
        pending = 0;
        Debug.Log($"[GarbageManager] Initialised — width={width}, config={(cfg != null ? cfg.name : "NULL")}, map={(targetMap != null ? targetMap.name : "NULL")}");
    }

    public void Reset() => pending = 0;

    /// <summary>
    /// Cancels up to <paramref name="attack"/> lines from the pending incoming garbage.
    /// Returns the overflow — lines remaining after all pending is cancelled.
    /// overflow = 0 means the attack was fully absorbed; overflow > 0 means a net counter-attack.
    /// </summary>
    public int CancelIncoming(int attack)
    {
        if (pending <= 0) return attack;
        int cancelled = Mathf.Min(attack, pending);
        pending -= cancelled;
        Debug.Log($"[GarbageManager] Cancelled {cancelled} incoming line(s), overflow={attack - cancelled}, remaining pending={pending}");
        return attack - cancelled;
    }

    public void Queue(int lines)
    {
        if (lines > 0)
        {
            pending += lines;
            Debug.Log($"[GarbageManager] Queued {lines} line(s), total pending={pending}");
        }
    }

    /// <summary>Call this in TetrisManager.OnNextTurn() before SpawnTetromino.</summary>
    public void InsertPending()
    {
        if (config == null)
        {
            Debug.LogWarning("[GarbageManager] InsertPending skipped — config is null");
            return;
        }
        if (pending <= 0)
            return;

        int count = pending;
        pending = 0;
        Debug.Log($"[GarbageManager] Inserting {count} garbage row(s), boundaryWidth={boundaryWidth}");

        int holeX = config.consistentHolePerWave
            ? Random.Range(0, boundaryWidth)
            : -1;

        ShiftRowsUp(count);
        map.FluidSystem.ShiftElementsUp(count);
        SpawnGarbageRows(count, holeX);
        AnimateGarbageRise(count);
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

    private void SpawnGarbageRows(int count, int holeX)
    {
        Debug.Log($"[GarbageManager] SpawnGarbageRows: count={count}, holeX={holeX}, blockID={config.garbageBlockID}");
        for (int row = 0; row < count; row++)
        {
            int rowHole = holeX >= 0 ? holeX : Random.Range(0, boundaryWidth);
            for (int x = 0; x < boundaryWidth; x++)
            {
                if (x == rowHole) continue;
                Block garb = BlockSpawner.NewBlock(config.garbageBlockID);
                map.RequestSpawnBlock(garb, x, row);
            }
            map.ImmediatelyProcessGridPendingUpdates();
            Debug.Log($"[GarbageManager] Spawned garbage row {row} (hole at x={rowHole})");
        }
    }
}
