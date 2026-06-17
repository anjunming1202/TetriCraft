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
