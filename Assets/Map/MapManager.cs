using System;
using System.Drawing;
using UnityEngine;

// Control the lifecycles of data in the map;
// Control logic of data (How but not When)
//      Control of the tetromino
//      Control map to e.g. clear one row, spawn tetrominos, ... 
//      ...
[Serializable]
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
        PlaceTetromino(fallingTetromino);

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
        MoveTetrominoTo(tetromino, tetromino.position + Vector2Int.down * distance);
    }



    //================================//
    //  Map Data Editing Operations
    //================================//

    /// <summary>
    /// Place a tetromino to another position
    /// </summary>
    private void MoveTetrominoTo(Tetromino tetromino, Vector2Int to)
    {
        // Remove original blocks
        RemoveTetromino(tetromino);

        // Move tetromino -> set position
        tetromino.position = to;

        // Place down tetromino blocks
        PlaceTetromino(tetromino);
    }
    /// <summary>
    /// Move a block to another position
    /// </summary>
    private void MoveBlockTo(Block block, Vector2Int to)
    {
        // Remove original block
        RemoveBlock(block);

        // Move block -> set position
        block.position = to;

        // Place down block
        PlaceBlock(block);
    }

    /// <summary>
    /// Place tetromino blocks onto the map
    /// </summary>
    private void PlaceTetromino(Tetromino tetromino)
    {
        // Getting blocks reference -> set block position by tetromino position + local position in the tetromino
        for (int c = 0; c < tetromino.size; c++)
            for (int r = 0; r < tetromino.size; r++)
            {
                Block block = tetromino[r, c];
                if (block != null)
                {
                    block.position = tetromino.LocalToMap(r, c);
                    PlaceBlock(block);
                }
            }
    }
    /// <summary>
    /// Place the block onto the map
    /// </summary>
    private void PlaceBlock(Block block)
    {
        Replace(block.position, block);
    }
    /// <summary>
    /// Replace one position block by another
    /// </summary>
    private void Replace(Vector2Int pos, Block block)
    {
        map[pos.x, pos.y] = block;
    }

    /// <summary>
    /// Remove tetromino blocks on the map
    /// </summary>
    private void RemoveTetromino(Tetromino tetromino)
    {
        foreach (Block block in tetromino.blocks)
        {
            RemoveBlock(block);
        }
    }
    /// <summary>
    /// Remove a block on the map
    /// </summary>
    private void RemoveBlock(Block block)
    {
        Remove(block.position);
    }
    /// <summary>
    /// Remove one position block
    /// </summary>
    private void Remove(Vector2Int pos)
    {
        map[pos.x, pos.y] = null;
    }



    //================================//
    //  Tetromino Control
    //================================//
    private bool TryMove(int x, int y)
    {
        fallingTetromino.Move(x, y);
        if (!CheckValid(fallingTetromino))
        {
            fallingTetromino.Move(-x, -y);
            Debug.Log(fallingTetromino.position);
            return false;
        }
        MoveTetrominoTo(fallingTetromino, fallingTetromino.position);

        return true;
    }
    public void Left()
    {
        TryMove(-1, 0);
    }
    public void Right()
    {
        TryMove(1, 0);
    }
    public void Fall()
    {
        bool successful = TryMove(0, -1);
        if (!successful)
        {
            OnLand();
        }
    }
    private void OnLand()
    {
        fallingTetromino.isLanded = true;
        foreach (var block in fallingTetromino.blocks)
        {
            block.isFalling = false;
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
    private bool CheckInside(Vector2Int pos)
    {
        return map.CheckInside(pos.x, pos.y);
    }
    /// <summary>
    /// Check for bottom, left, and right boundaries
    /// </summary>
    private bool CheckInside(Tetromino tetromino)
    {
        for (int r = 0; r < tetromino.size; r++)
            for (int c = 0; c < tetromino.size; c++)
            {
                if (tetromino[r, c] != null)
                {
                    Vector2Int blockPos = tetromino.LocalToMap(r, c);
                    if (!CheckInside(blockPos))
                        return false;
                }
            }
        return true;
    }
    private bool CheckCollide(Tetromino tetromino)
    {
        for (int r = 0; r < tetromino.size; r++)
            for (int c = 0; c < tetromino.size; c++)
            {
                if (tetromino[r, c] != null)
                {
                    Vector2Int mapBlockPos = tetromino.LocalToMap(r, c);
                    Block mapBlock = map[mapBlockPos.x, mapBlockPos.y];
                    if (mapBlock != null && !mapBlock.isFalling)
                    {
                        Debug.Log("Collide");
                        return true;
                    }
                }
            }
        return false;
    }
    private bool CheckValid(Tetromino tetromino)
    {
        // Check inside first, check not collide then
        return (CheckInside(tetromino) && !CheckCollide(tetromino));
    }
}
