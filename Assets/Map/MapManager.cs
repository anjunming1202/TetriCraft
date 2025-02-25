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
        map.SetBlocksPosition(fallingTetromino);

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
        map.SetTetrominoPosition(tetromino, tetromino.position + Vector2Int.down * distance);
    }


    //================================//
    //  Tetromino Control
    //================================//
    public bool Move(int x, int y)
    {
        fallingTetromino.Move(x, y);
        if (!map.CheckInside(fallingTetromino))
        {
            fallingTetromino.Move(-x, -y);
            Debug.Log(fallingTetromino.position);
            return false;
        }
        map.SetBlocksPosition(fallingTetromino);

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
}
