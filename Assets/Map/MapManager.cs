using System;
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




    public void NewMap()
    {
        // New a map
        map = new Map();

        // Prepare next tetromino
        CreateNextTetromino();
    }

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
        AddTetromino(fallingTetromino);

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



    private void AddTetromino(Tetromino tetromino)
    {
        tetromino.MoveTo(map, tetromino.position);
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
        tetromino.MoveTo(map, tetromino.position + Vector2Int.down * distance);
    }
}
