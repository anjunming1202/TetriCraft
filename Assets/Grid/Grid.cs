using Unity.Mathematics;
using UnityEngine;

public class Grid
{
    public Grid()
    {
        width = GridBoundaryData.Instance.width;
        height = GridBoundaryData.Instance.height;
        grid = new Block[width, height + 5]; // all null
    }

    // Grid data
    private int width;
    private int height;
    public Block[,] grid;
    public Block this[int x, int y]
    {
        get => grid[x, y];
        set => grid[x, y] = value;
    }

    public void AddTetromino(Tetromino tetromino)
    {
        tetromino.GoTo(this, tetromino.position);
    }
    public bool CheckInside(Tetromino tetromino)
    {
        return true;
    }
    /*
    public void AddBlock(Block block)
    {
        grid[block.position.x, block.position.y] = block;
    }
    private void MoveGrid(Vector2Int from, Vector2Int to)
    {
        grid[to.x, to.y] = grid[from.x, from.y];
        grid[from.x, from.y] = null;
    }*/
}
