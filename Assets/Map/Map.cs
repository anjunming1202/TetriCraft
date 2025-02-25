using System.Drawing;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Data of blocks in the game
/// </summary>
public class Map
{
    public Map()
    {
        blockMap = new Block[width, height + 5]; // all null
    }

    public Block[,] blockMap;

    // Map Boundary Data
    private int width => MapBoundaryData.Instance.width;
    private int height => MapBoundaryData.Instance.height;
    public Block this[int x, int y]
    {
        get => blockMap[x, y];
        set => blockMap[x, y] = value;
    }
        
    /// <summary>
    /// Check for bottom, left, and right boundaries
    /// </summary>
    public bool CheckInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0;
    }
}
