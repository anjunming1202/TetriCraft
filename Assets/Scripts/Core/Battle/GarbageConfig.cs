using UnityEngine;

[CreateAssetMenu(fileName = "GarbageConfig", menuName = "Battle/GarbageConfig")]
public class GarbageConfig : ScriptableObject
{
    [Header("Attack Table (index = lines cleared)")]
    [SerializeField] public int[] attackTable = { 0, 0, 1, 2, 4 };

    [Header("Combo Bonus Table (index = combo count)")]
    [SerializeField] public int[] comboBonusTable = { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 5 };

    [Header("Garbage Block")]
    [SerializeField] public BlockID garbageBlockID = BlockID.Garbage;

    [Tooltip("All rows in the same garbage wave share one hole X position")]
    [SerializeField] public bool consistentHolePerWave = true;

    /// <summary>Returns the number of garbage lines to send given lines cleared and current combo.</summary>
    public int CalculateGarbage(uint linesCleared, uint combo)
    {
        int idx = Mathf.Min((int)linesCleared, attackTable.Length - 1);
        int fromLines = attackTable[idx];
        int comboIdx = Mathf.Min((int)combo, comboBonusTable.Length - 1);
        int fromCombo = combo > 0 ? comboBonusTable[comboIdx] : 0;
        return fromLines + fromCombo;
    }
}
