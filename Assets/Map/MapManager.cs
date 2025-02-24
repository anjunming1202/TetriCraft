using System;
using UnityEngine;

// Manage and control the lifecycles of data in the map;
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

        // Set tetromino position (at the centre)
        int x = (boundary.width - fallingTetromino.size) / 2;
        fallingTetromino.position = new Vector2Int(x, boundary.height);

        // Rotate tetromino randomly

        // Place tetromino to spawn point
        AddTetromino(fallingTetromino);

        // Generate next tetromino with random type
        CreateNextTetromino();
    }
    private void CreateNextTetromino()
    {
        // Random type
        CreateNextTetromino(TetrominoType.T);//
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
}
