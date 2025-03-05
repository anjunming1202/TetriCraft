using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using static Unity.Collections.AllocatorManager;
using static UnityEditor.PlayerSettings;

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

    // Recorded data
    public int lineCount = 0; // combo for clear by landing tetromino
    public int combo = 0;
    public int softDrop = 0;
    public int hardDrop = 0;

    // Readonly Data
    private MapBoundaryData boundary => MapBoundaryData.Instance; // Boundary Data

    // Event: notify the game manager
    public delegate void MapEvent(MapManager mapManager);
    public MapEvent OnTetrominoLocked;
    public MapEvent OnTetrominoSoftDrop; // for player controlled drop: accelerate (soft drop)
    public MapEvent OnTetrominoHardDrop; // for player controlled drop: land (hard drop)
    public MapEvent OnLineClear;


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
        // Initialise next falling tetromino & its blocks
        fallingTetromino = nextTetromino;
        InitialiseNewTetromino(fallingTetromino);

        // Set tetromino x position
        int x = (boundary.width - fallingTetromino.size) / 2;
        fallingTetromino.SetPosition(new Vector2Int(x, boundary.height));

        // Set tetromino x position
        SetSittingOnCeiling(fallingTetromino);

        // Rotate tetromino randomly

        // Generate next tetromino with random type
        CreateNextTetromino();
    }



    private void InitialiseNewTetromino(Tetromino tetromino)
    {
        // reset tetromino data in map
        lineCount = 0;
        softDrop = 0;
        hardDrop = 0;

        // set up tetromino
        fallingTetromino.isActive = true;
        fallingTetromino.isLocked = false;

        // set up blocks
        foreach (var block in tetromino.blocks)
        {
            InitialiseBlock(block);
        }

        // tetromino lockdown (land) -> try clear line
        fallingTetromino.OnLockdown += TryClearLine;
    }
    private void InitialiseBlock(Block block)
    {
        // block on spawn falling
        block.SpawnFalling();
        /*// block land -> try clear line
        block.OnLanded += TryClearLine;*/
    }
    private void CreateNextTetromino()
    {
        // Random tetromino type
        TetrominoType tetroType = (TetrominoType)UnityEngine.Random.Range(0, (int)TetrominoType.Count);

        // Random blocks type
        BlockType blockType = BlockRandomiser.GetRandomType();

        CreateNextTetromino(tetroType, blockType);
    }
    private void CreateNextTetromino(TetrominoType tetroType, BlockType blockType)
    {
        // For intrinsic tetromino (same four blocks)
        Block[] blocks = new Block[4];
        for (int i = 0; i < 4; i++)
        {
            blocks[i] = BlockFactory.CreateBlock(blockType);
        }

        // New a tetromino
        nextTetromino = new Tetromino(tetroType, blocks[0], blocks[1], blocks[2], blocks[3]);
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
        // Set tetromino to spawn point
        SetTetromino(tetromino, tetromino.MapPosition + Vector2Int.down * distance);
    }



    //================================//
    //  Game State Check
    //================================//
    public bool CheckGameover()
    {
        return !map.IsRowEmpty(boundary.height);
    }



    //================================//
    //  Tetromino Control
    //================================//
    public void Left()
    {
        TryMove(-1, 0);
    }
    public void Right()
    {
        TryMove(1, 0);
    }
    public void Drop()
    {
        bool successful = TryMove(0, -1);
        if (!successful)
        {
            Lockdown();
        }
    }
    public void SoftDrop()
    {
        softDrop++;
        OnTetrominoSoftDrop?.Invoke(this);
        Drop();
    }
    public void HardDrop()
    {
        while (TryMove(0, -1)) 
        {
            hardDrop++;
        }
        OnTetrominoHardDrop?.Invoke(this);
        Lockdown();
    }
    public void Rotate(bool clockwise = true)
    {
        TryRotate(clockwise);
    }



    /// <summary>
    /// Tetromino lockdown
    /// </summary>
    private void Lockdown()
    {
        // update tetromino & blocks data
        fallingTetromino.Lockdown();

        // invoke map tetromino landing event
        OnTetrominoLocked?.Invoke(this);
    }
    private bool TryMove(int x, int y)
    {
        fallingTetromino.MoveBy(x, y);
        if (!CheckValid(fallingTetromino))
        {
            fallingTetromino.MoveBy(-x, -y);
            Debug.Log("fail fall");
            return false;
        }
        MoveTetrominoTo(fallingTetromino, fallingTetromino.MapPosition);
        return true;
    }
    private bool TryRotate(bool clockwise = true)
    {
        fallingTetromino.Rotate(clockwise);
        // check for each wall kick position
        foreach (Vector2Int kick in fallingTetromino.Wallkick())
        {
            fallingTetromino.MoveBy(kick.x, kick.y);
            if (CheckValid(fallingTetromino))
            {
                MoveTetrominoTo(fallingTetromino, fallingTetromino.MapPosition);
                Debug.Log("success rotation");
                return true;
            }
            fallingTetromino.MoveBy(-kick.x, -kick.y);
        }
        fallingTetromino.Rotate(!clockwise);
        Debug.Log("fail rotation");
        return false;
    }



    //================================//
    //  Line Clear
    //================================//

    /// <summary>
    /// Try clear line for tetromino when landing
    /// </summary>
    private void TryClearLine(Tetromino tetromino)
    {
        int count = 0;
        foreach (var block in tetromino.blocks)
        {
            bool successful = TryClearLine(block.MapPosition.y);
            if (successful)
                count++;
        }

        if (count > 0)
        {
            lineCount = count;
            combo++;
            OnLineClear?.Invoke(this);
        }
        else
        {
            // reset combo for fail clear of tetromino landing only
            combo = 0;
        }
    }
    /// <summary>
    /// For block that want to try clear line at time other than landing
    /// </summary>
    private void TryClearLine(Block block)
    {
        bool successful = TryClearLine(block.MapPosition.y);

        if (successful)
        {
            lineCount = 1;
            combo++;
            OnLineClear?.Invoke(this);
        }
    }



    private bool TryClearLine(int row)
    {
        if (CheckFull(row))
        {
            ClearLine(row);
            return true;
        }
        return false;
    }
    private void ClearLine(int row)
    {
        // clear row
        DestroyLine(row);
        // move above rows down
        for (int x = 0; x < map.width; x++)
            for (int y = row + 1; y < map.height; y++)  // * must from bottom to top
            {
                if (!IsEmpty(x, y))
                {
                    map.MoveBlockTo(map[x, y], new Vector2Int(x, y - 1));
                }
            }
    }

    /// <summary>
    /// Destroy a row of blocks and leave it empty
    /// </summary>
    /// <param name="row"></param>
    private void DestroyLine(int row)
    {
        for (int i = 0; i < map.width; i++)
        {
            map.Destroy(i, row);
        }
    }



    //================================//
    // Map Data Editting with Tetromino
    //================================//

    private void MoveTetrominoTo(Tetromino tetromino, Vector2Int to)
    {
        // Remove original blocks
        RemoveTetromino(tetromino);

        // Move tetromino -> set position
        tetromino.MoveTo(to);

        // Place down tetromino blocks with moving
        PlaceTetromino(tetromino);
    }

    /// <summary>
    /// Place down the tetromino according to its position data
    /// </summary>
    private void PlaceTetromino(Tetromino tetromino)
    {
        // Set data of blocks (in the map + block self)
        foreach (Block block in tetromino.blocks)
        {
            map.PlaceBlock(block);
        }
    }

    /// <summary>
        /// Set tetromino blocks onto the map
        /// </summary>
    private void SetTetromino(Tetromino tetromino, int x, int y)
    {
        // Set data of tetromino self
        tetromino.SetPosition(new Vector2Int(x, y));

        // Set data of blocks (in the map + block self)
        for (int c = 0; c < tetromino.size; c++)
            for (int r = 0; r < tetromino.size; r++)
            {
                Block block = tetromino[r, c];
                if (block != null)
                {
                    // block position = tetromino position + local position in the tetromino
                    Vector2Int blockPosition = tetromino.LocalToMap(r, c);
                    map.PlaceBlock(block, blockPosition);
                }
            }
    }
    /// <summary>
    /// Set tetromino blocks onto the map
    /// </summary>
    private void SetTetromino(Tetromino tetromino, Vector2Int pos) => SetTetromino(tetromino, pos.x, pos.y);

    /// <summary>
    /// Remove tetromino blocks on the map, but not destroy
    /// </summary>
    private void RemoveTetromino(Tetromino tetromino)
    {
        foreach (Block block in tetromino.blocks)
        {
            map.Remove(block);
        }
    }



    //================================//
    //  Map Data Checking
    //================================//

    private bool IsEmpty(int x, int y)
    {
        return map.IsEmpty(x, y);
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
        return map.IsRowFull(row);
    }
}
