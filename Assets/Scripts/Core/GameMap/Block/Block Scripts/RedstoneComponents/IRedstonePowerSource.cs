using UnityEngine;

public interface IRedstonePowerSource
{
    /// <summary>
    /// Whether this block emits power toward neighborPos.
    /// RedstoneBlock always returns true; future blocks (Observer) may filter by direction.
    /// </summary>
    bool PowersPosition(Vector2Int myPos, Vector2Int neighborPos);
}
