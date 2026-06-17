using System;
using UnityEngine;

/// <summary>
/// Extends TetrisManager with battle-specific behaviour (garbage line system).
/// Use this component on each player's TetrisManager GameObject in the battle scene.
/// </summary>
public class BattleTetrisManager : TetrisManager
{
    [Header("Battle")]
    [SerializeField] private GarbageManager garbageManager;

    /// <summary>
    /// Fired at the end of each lockdown, carrying the completed cycle's stats.
    /// cycleClears = all lines cleared since the previous lockdown (immediate + fluid).
    /// cycleCombo  = combo value before this lockdown's combo update.
    /// </summary>
    public event Action<uint, uint> OnTurnLineClearsComplete;

    public void SetGarbageConfig(GarbageConfig cfg)
        => garbageManager.Initialise(boundaryWidth, cfg, Map);

    public void QueueGarbage(int lines, uint sourceClears = 0, uint sourceCombo = 0)
        => garbageManager.Queue(lines, sourceClears, sourceCombo);

    /// <summary>
    /// Attempts to cancel <paramref name="attack"/> lines of incoming garbage with a line-clear attack.
    /// Returns the overflow — lines that exceed the pending garbage and can be sent to the opponent.
    /// </summary>
    public int CancelIncomingGarbage(int attack) => garbageManager.CancelIncoming(attack);

    protected override void OnLockdown()
    {
        // Read previous cycle stats before base resets totalClearLineCount and updates combo
        uint savedClears = totalClearLineCount;
        uint savedCombo  = combo;

        base.OnLockdown(); // resets totalClearLineCount, runs TryClearLines, updates combo, fires OnFinishedTurn

        OnTurnLineClearsComplete?.Invoke(savedClears, savedCombo);
    }

    protected override void OnNextTurn()
    {
        garbageManager.InsertPending();
        base.OnNextTurn();
    }
}
