using Unity.Mathematics;
using UnityEngine;

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

    public bool CheckInside(Tetromino tetromino)
    {
        return true;
    }
    /*
    public void AddBlock(Block block)
    {
        blockMap[block.position.x, block.position.y] = block;
    }
    private void MoveGrid(Vector2Int from, Vector2Int to)
    {
        blockMap[to.x, to.y] = blockMap[from.x, from.y];
        blockMap[from.x, from.y] = null;
    }*/
}
