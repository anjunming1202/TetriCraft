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
    public int width => MapBoundaryData.Instance.width;
    public int height => MapBoundaryData.Instance.height;
    public Block this[int x, int y]
    {
        get => blockMap[x, y];
        set => blockMap[x, y] = value;
    }
        
    /// <summary>
    /// Check for bottom, left, and right boundaries
    /// </summary>
    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0;
    }

    public bool IsFull(int row)
    {
        for (int column = 0; column < width; column++)
        {
            if (blockMap[column, row] == null)
                return false;
        }
        return true;
    }
    public bool IsEmpty(int row)
    {
        for (int column = 0; column < width; column++)
        {
            if (blockMap[column, row] != null)
                return false;
        }
        return true;
    }
}
