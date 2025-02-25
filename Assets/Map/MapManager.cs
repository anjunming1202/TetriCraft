using System;
using System.Drawing;
using UnityEngine;

// Control the lifecycles of data in the map;
// Control logic of data (How but not When)
//      Control of the tetromino
//      Control map to e.g. clear one row, spawn tetrominos, ... 
//      ...
public class MapManager : MonoBehaviour
{
    // Map Data
    public Map map;

    // Tetromino
    private Tetromino fallingTetromino;
    private Tetromino nextTetromino;
    public Tetromino CurrentTetromino => fallingTetromino;

    // Readonly Data
    private MapBoundaryData boundary => MapBoundaryData.Instance; // Boundary Data



    //================================//
    //  Initialise Map
    //================================//
    public void NewMap()
    {
        // New a map
        map = new Map();

        // Prepare next tetromino
        CreateNextTetromino();
    }



    //================================//
    //  Initialise Tetromino
    //================================//
    public void SpawnTetromino()
    {
        // Initialise next falling tetromino (nextTetromino should be prepared anytime)
        fallingTetromino = nextTetromino;
        fallingTetromino.isActive = true;

        // Set tetromino position (at the centre & sitting on ceiling)
        int x = (boundary.width - fallingTetromino.size) / 2;
        fallingTetromino.position = new Vector2Int(x, boundary.height);
        SetSittingOnCeiling(fallingTetromino);

        // Rotate tetromino randomly

        // Place tetromino to spawn point
        PlaceTetrominoBlocks(fallingTetromino);

        // Generate next tetromino with random type
        CreateNextTetromino();
    }

    private void CreateNextTetromino()
    {
        // Random type
        TetrominoType type = (TetrominoType)UnityEngine.Random.Range(0, (int)TetrominoType.Count);
        CreateNextTetromino(type);
    }

    public void CreateNextTetromino(TetrominoType type)
    {
        // New a tetromino
        nextTetromino = new Tetromino(type);
    }

    private void SetSittingOnCeiling(Tetromino tetromino)
    {
        // distance tetromino need to move
        int distance = int.MaxValue;
        for (int r = 0; r < tetromino.size; r++)
            for (int c = 0; c < tetromino.size; c++)
            {
                if (tetromino[r, c] != null)
                {
                    int distance_new = tetromino.LocalToMap(r, c).y - boundary.height;
                    if (distance_new < distance)
                        distance = distance_new;
                }
            }
        // move tetromino downwards
        PlaceTetromino(tetromino, tetromino.position + Vector2Int.down * distance);
    }



    //================================//
    //  Map Data Editing
    //================================//
    /// <summary>
    /// Place a tetromino to a position
    /// </summary>
    private void PlaceTetromino(Tetromino tetromino, Vector2Int position)
    {
        // Set tetromino data -> set position
        tetromino.position = position;
        // Set block data & update map data
        PlaceTetrominoBlocks(tetromino);
    }

    private void PlaceTetrominoBlocks(Tetromino tetromino)
    {
        // Getting blocks reference -> set block position by tetromino position + local position in the tetromino
        for (int c = 0; c < tetromino.size; c++)
            for (int r = 0; r < tetromino.size; r++)
            {
                Block block = tetromino[r, c];
                if (block != null)
                {
                    MoveBlock(block, tetromino.LocalToMap(r, c));
                }
            }
    }
    /// <summary>
    /// Move a block to a position
    /// </summary>
    private void MoveBlock(Block block, Vector2Int to)
    {
        Vector2Int from = block.position;
        // Set block data
        block.position = to;
        // Set map data
        map[from.x, from.y] = null;
        map[to.x, to.y] = block;
    }



    //================================//
    //  Tetromino Control
    //================================//
    private bool TryMove(int x, int y)
    {
        fallingTetromino.Move(x, y);
        if (!CheckInside(fallingTetromino))
        {
            fallingTetromino.Move(-x, -y);
            Debug.Log(fallingTetromino.position);
            return false;
        }
        PlaceTetrominoBlocks(fallingTetromino);

        return true;
    }
    public void MoveLeft()
    {
        TryMove(-1, 0);
    }
    public void MoveRight()
    {
        TryMove(1, 0);
    }
    public void MoveDown()
    {
        bool successful = TryMove(0, -1);
        if (!successful)
        {
            fallingTetromino.landed = true;
        }
    }
    public void Land()
    {
        
    }

    public void Rotate(bool isclockwise = true)
    {

    }


    

    /// <summary>
    /// Check for bottom, left, and right boundaries
    /// </summary>
    public bool CheckInside(Tetromino tetromino)
    {
        for (int r = 0; r < tetromino.size; r++)
            for (int c = 0; c < tetromino.size; c++)
            {
                if (tetromino[r, c] != null)
                {
                    Vector2Int blockPos = tetromino.LocalToMap(r, c);
                    if (!map.CheckInside(blockPos.x, blockPos.y))
                        return false;
                }
            }
        return true;
    }
}
