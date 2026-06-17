using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Passed to <see cref="GarbageConfig.GetGarbageLayout"/> so the config can tailor
/// the layout to the incoming attack's context.
/// </summary>
public struct GarbageInsertContext
{
    /// <summary>Net garbage lines to insert.</summary>
    public int totalRows;
    /// <summary>Width of the player's board.</summary>
    public int boardWidth;
    /// <summary>Lines cleared by the attacker this cycle.</summary>
    public uint sourceClears;
    /// <summary>Combo count of the attacker this cycle.</summary>
    public uint sourceCombo;
}

[CreateAssetMenu(fileName = "GarbageConfig", menuName = "Battle/GarbageConfig")]
public class GarbageConfig : ScriptableObject
{
    [Header("Attack Table (index = lines cleared)")]
    [SerializeField] public int[] attackTable = { 0, 0, 1, 2, 4 };

    [Header("Combo Bonus Table (index = combo count)")]
    [SerializeField] public int[] comboBonusTable = { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 5 };

    [Header("Garbage Block")]
    [SerializeField] public BlockID garbageBlockID = BlockID.Garbage;

    [Header("Garbage Hole")]
    [SerializeField] private int holeCount = 1;
    [Tooltip("All rows in the same garbage wave share one set of hole columns")]
    [SerializeField] private bool consistentHolePerWave = true;

    /// <summary>Returns the number of garbage lines to send given lines cleared and current combo.</summary>
    public int CalculateGarbage(uint linesCleared, uint combo)
    {
        int idx = Mathf.Min((int)linesCleared, attackTable.Length - 1);
        int fromLines = attackTable[idx];
        int comboIdx = Mathf.Min((int)combo, comboBonusTable.Length - 1);
        int fromCombo = combo > 0 ? comboBonusTable[comboIdx] : 0;
        return fromLines + fromCombo;
    }

    /// <summary>
    /// Returns the block layout for a garbage wave.
    /// result[row, x] == null means a hole; any BlockID value means spawn that block there.
    /// Override in a subclass to implement custom patterns (e.g. special-clear layouts).
    /// </summary>
    public virtual BlockID?[,] GetGarbageLayout(GarbageInsertContext ctx)
    {
        var layout = new BlockID?[ctx.totalRows, ctx.boardWidth];

        int[] sharedHoles = consistentHolePerWave ? PickHoles(ctx.boardWidth) : null;

        for (int row = 0; row < ctx.totalRows; row++)
        {
            int[] holes = sharedHoles ?? PickHoles(ctx.boardWidth);
            var holeSet = new HashSet<int>(holes);
            for (int x = 0; x < ctx.boardWidth; x++)
                layout[row, x] = holeSet.Contains(x) ? (BlockID?)null : garbageBlockID;
        }
        return layout;
    }

    private int[] PickHoles(int boardWidth)
    {
        int count = Mathf.Min(holeCount, boardWidth - 1); // always leave at least 1 solid cell
        var result = new int[count];
        var used = new HashSet<int>();
        int i = 0;
        while (i < count)
        {
            int x = Random.Range(0, boardWidth);
            if (used.Add(x)) result[i++] = x;
        }
        return result;
    }
}
