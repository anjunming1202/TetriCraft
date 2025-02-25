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
    //  Initialise
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
        SetBlocksPosition(fallingTetromino);

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
        SetTetrominoPosition(tetromino, tetromino.position + Vector2Int.down * distance);
    }


    //================================//
    //  Tetromino Control
    //================================//
    public bool Move(int x, int y)
    {
        fallingTetromino.Move(x, y);
        if (!CheckInside(fallingTetromino))
        {
            fallingTetromino.Move(-x, -y);
            Debug.Log(fallingTetromino.position);
            return false;
        }
        SetBlocksPosition(fallingTetromino);

        return true;
    }
    public bool Left()
    {
        Move(-1, 0);

        return true;
    }
    public bool Right()
    {
        Move(1, 0);

        return true;
    }

    public bool Fall()
    {
        Move(0, -1);

        return true;
    }

    public bool Accelerate()
    {
        Move(0, -1);

        return true;
    }

    public bool Rotate(bool isclockwise = true)
    {
        return true;
    }

    public bool Land()
    {
        return true;
    }


    //================================//
    //  Set map data
    //================================//

    /// <summary>
    /// Set position of tetromino, position of blocks contained, and update block map.
    /// </summary>
    public void SetTetrominoPosition(Tetromino tetromino, Vector2Int position)
    {
        // Set tetromino data -> set position
        tetromino.position = position;
        // Set block data & update map data
        SetBlocksPosition(tetromino);
    }
    /// <summary>
    /// Set position of blocks contained and update block map, based on current tetromino data
    /// </summary>
    public void SetBlocksPosition(Tetromino tetromino)
    {
        // Getting blocks reference -> set block position by tetromino position + local position in the tetromino
        for (int c = 0; c < tetromino.size; c++)
            for (int r = 0; r < tetromino.size; r++)
            {
                Block block = tetromino[r, c];
                if (block != null)
                {
                    Vector2Int to = tetromino.LocalToMap(r, c);
                    Vector2Int from = block.position;
                    // Set block data
                    block.position = to;
                    // Set map data
                    map[from.x, from.y] = null;
                    map[to.x, to.y] = block;
                }
            }
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
