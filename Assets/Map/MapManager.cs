using System;
using System.Collections.Generic;
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

    // Clear Line
    public int lastCombo = 0; // combo for clear by landing tetromino

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

    private void InitialiseNewTetromino(Tetromino tetromino)
    {
        // set up tetromino
        fallingTetromino.isActive = true;
        fallingTetromino.isLanded = false;
        fallingTetromino.OnLanded += TryClearLine;

        // set up blocks
        foreach (var block in tetromino.blocks)
        {
            InitialiseBlock(block);
        }
    }
    private void InitialiseBlock(Block block)
    {
        block.SpawnFalling();
    }
    public void SpawnTetromino()
    {
        // Initialise next falling tetromino & its blocks (nextTetromino should be prepared anytime)
        fallingTetromino = nextTetromino;
        InitialiseNewTetromino(fallingTetromino);

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
    //  Tetromino Control
    //================================//
    private bool TryMove(int x, int y)
    {
        fallingTetromino.Move(x, y);
        if (!CheckValid(fallingTetromino))
        {
            fallingTetromino.Move(-x, -y);
            Debug.Log("fail fall");
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
    public void Land()
    {
        while (TryMove(0, -1)) { }
        OnLand();
    }
    private void OnLand()
    {
        // update tetromino & blocks data
        fallingTetromino.Land();
    }

    private bool TryRotate(bool clockwise = true)
    {
        fallingTetromino.Rotate(clockwise);
        // check for each wall kick position
        foreach (Vector2Int kick in fallingTetromino.Wallkick())
        {
            fallingTetromino.Move(kick.x, kick.y);
            if (CheckValid(fallingTetromino))
            {
                MoveTetrominoTo(fallingTetromino, fallingTetromino.position);
                Debug.Log("success rotation");
                return true;
            }
            fallingTetromino.Move(-kick.x, -kick.y);
        }
        fallingTetromino.Rotate(!clockwise);
        Debug.Log("fail rotation");
        return false;
    }
    public void Rotate(bool clockwise = true)
    {
        TryRotate(clockwise);
    }



    //================================//
    //  Line Clear
    //================================//

    /// <summary>
    /// Try clear line for tetromino when landing
    /// </summary>
    private void TryClearLine(Tetromino tetromino)
    {
        int combo = 0;
        foreach (var block in tetromino.blocks)
        {
            bool successful = TryClearLine(block.position.y);
            if (successful)
                combo++;
        }

        if (combo > 0)
        {
            lastCombo = combo;
            // trigger on clear line
        }
    }
    /// <summary>
    /// For block that want to try clear line at time other than landing
    /// </summary>
    private void TryClearLine(Block block)
    {
        bool successful = TryClearLine(block.position.y);

        if (successful)
        {
            lastCombo = 1;
            // trigger on clear line
        }
    }
    private bool TryClearLine(int row)
    {
        if (CheckFull(row))
        {
            // clear row
            DestroyLine(row);
            // move above rows down
            for (int x = 0; x < map.width; x++)
                for (int y = row + 1; y < map.height; y++)  // * must from bottom to top
                {
                    if (!IsEmpty(x, y))
                    {
                        MoveBlockTo(map[x, y], new Vector2Int(x, y - 1));
                    }
                }
            return true;
        }
        return false;
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

        // Move block
        block.position = to; // set position

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
        if (!block.isInMap)
            block.isInMap = true;
    }
    /// <summary>
    /// Replace one position block by another
    /// </summary>
    private void Replace(Vector2Int pos, Block block)
    {
        map[pos.x, pos.y] = block;
    }

    /// <summary>
    /// Remove tetromino blocks on the map, but not destroy
    /// </summary>
    private void RemoveTetromino(Tetromino tetromino)
    {
        foreach (Block block in tetromino.blocks)
        {
            RemoveBlock(block);
        }
    }
    /// <summary>
    /// Remove a block on the map, but not destroy
    /// </summary>
    private void RemoveBlock(Block block)
    {
        // if block not be considered in the map, skip
        if (block.isInMap)
            Remove(block.position);
    }
    /// <summary>
    /// Remove one position block, but not destroy
    /// </summary>
    private void Remove(Vector2Int pos)
    {
        map[pos.x, pos.y] = null;
    }
    /// <summary>
    /// Remove one position block, but not destroy
    /// </summary>
    private void Remove(int x, int y)
    {
        map[x, y] = null;
    }

    /// <summary>
    /// Destroy then remove block
    /// </summary>
    private void Destroy(int x, int y)
    {
        map[x, y].Destroy();
        Remove(x, y);
    }
    /// <summary>
    /// Destroy a row of blocks and leave it empty
    /// </summary>
    /// <param name="row"></param>
    private void DestroyLine(int row)
    {
        for (int i = 0; i < map.width; i++)
        {
            Destroy(i, row);
        }
    }



    private bool IsEmpty(int x, int y)
    {
        return map[x, y] == null;
    }

    /// <summary>
    /// Check for bottom, left, and right boundaries
    /// </summary>
    private bool CheckInside(Vector2Int pos)
    {
        return map.IsInside(pos.x, pos.y);
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

    private bool CheckFull(int row)
    {
        return map.IsFull(row);
    }
}
