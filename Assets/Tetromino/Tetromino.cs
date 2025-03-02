// Four blocks are one tetromino
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

// A tetromino stores 4 blocks, when falling
[Serializable]
public class Tetromino
{
    public Tetromino(TetrominoType type) : this(type, new NullBlock(), new NullBlock(), new NullBlock(), new NullBlock()) { }
    // public Tetromino(TetrominoType type, BlockType blockType) : this(type, block, block, block, block) { }
    public Tetromino(TetrominoType type, Block block1, Block block2, Block block3, Block block4)
    {
        // Initialise tetromino data
        Type = type;
        switch (type)
        {
            // * origin [0,0] at left bottom, but when defining [,] array the upper row of code should be the lower row in tetromino
            case TetrominoType.I:
                size = 4;
                shape = new Block[4, 4] {
                { null, null, null, null },
                { null, null, null, null },
                { block1, block2, block3, block4 },
                { null, null, null, null } };
                break;
            case TetrominoType.O:
                shape = new Block[4, 4] {
                { null, null, null, null },
                { null, block3, block4, null },
                { null, block1, block2, null },
                { null, null, null, null } };
                size = 4;
                break;
            case TetrominoType.T:
                shape = new Block[3, 3] {
                { null, null, null },
                { block2, block3, block4 },
                { null, block1, null } };
                size = 3;
                break;
            case TetrominoType.J:
                shape = new Block[3, 3] {
                { null, null, null },
                { block2, block3, block4 },
                { block1, null, null } };
                size = 3;
                break;
            case TetrominoType.L:
                shape = new Block[3, 3] {
                { null, null, null },
                { block2, block3, block4 },
                { null, null, block1 } };
                size = 3;
                break;
            case TetrominoType.S:
                shape = new Block[3, 3] {
                { null, null, null },
                { block3, block4, null },
                { null, block1, block2 } };
                size = 3;
                break;
            case TetrominoType.Z:
                shape = new Block[3, 3] {
                { null, null, null },
                { null, block3, block4 },
                { block1, block2, null } };
                size = 3;
                break;
        }

        // Initialise wall kick data
        wallkick = new Dictionary<Vector2Int, Vector2Int[]>(8);
        switch (type)
        {
            case TetrominoType.I:
                wallkick[new Vector2Int(0, 1)] = new Vector2Int[5] { new(0, 0), new(-2, 0), new( 1, 0), new(-2,-1), new( 1, 2) };
                wallkick[new Vector2Int(1, 0)] = new Vector2Int[5] { new(0, 0), new( 2, 0), new(-1, 0), new( 2, 1), new(-1,-2) };
                wallkick[new Vector2Int(1, 2)] = new Vector2Int[5] { new(0, 0), new(-1, 0), new( 2, 0), new(-1, 2), new( 2,-1) };
                wallkick[new Vector2Int(2, 1)] = new Vector2Int[5] { new(0, 0), new( 1, 0), new(-2, 0), new( 1,-2), new(-2, 1) };
                wallkick[new Vector2Int(2, 3)] = new Vector2Int[5] { new(0, 0), new( 2, 0), new(-1, 0), new( 2, 1), new(-1,-2) };
                wallkick[new Vector2Int(3, 2)] = new Vector2Int[5] { new(0, 0), new(-2, 0), new( 1, 0), new(-2,-1), new( 1, 2) };
                wallkick[new Vector2Int(3, 0)] = new Vector2Int[5] { new(0, 0), new( 1, 0), new(-2, 0), new( 1,-2), new(-2, 1) };
                wallkick[new Vector2Int(0, 3)] = new Vector2Int[5] { new(0, 0), new(-1, 0), new( 2, 0), new(-1, 2), new( 2,-1) };
                break;
            case TetrominoType.O:
                wallkick[new Vector2Int(0, 1)] = new Vector2Int[1] { new(0, 0) };
                wallkick[new Vector2Int(1, 0)] = new Vector2Int[1] { new(0, 0) };
                wallkick[new Vector2Int(1, 2)] = new Vector2Int[1] { new(0, 0) };
                wallkick[new Vector2Int(2, 1)] = new Vector2Int[1] { new(0, 0) };
                wallkick[new Vector2Int(2, 3)] = new Vector2Int[1] { new(0, 0) };
                wallkick[new Vector2Int(3, 2)] = new Vector2Int[1] { new(0, 0) };
                wallkick[new Vector2Int(3, 0)] = new Vector2Int[1] { new(0, 0) };
                wallkick[new Vector2Int(0, 3)] = new Vector2Int[1] { new(0, 0) };
                break;
            case TetrominoType.T:
            case TetrominoType.J:
            case TetrominoType.L:
            case TetrominoType.S:
            case TetrominoType.Z:
                wallkick[new Vector2Int(0, 1)] = new Vector2Int[5] { new(0, 0), new(-1, 0), new(-1, 1), new( 0,-2), new(-1,-2) };
                wallkick[new Vector2Int(1, 0)] = new Vector2Int[5] { new(0, 0), new( 1, 0), new( 1,-1), new( 0, 2), new( 1, 2) };
                wallkick[new Vector2Int(1, 2)] = new Vector2Int[5] { new(0, 0), new( 1, 0), new( 1,-1), new( 0, 2), new( 1, 2) };
                wallkick[new Vector2Int(2, 1)] = new Vector2Int[5] { new(0, 0), new(-1, 0), new(-1, 1), new( 0,-2), new(-1,-2) };
                wallkick[new Vector2Int(2, 3)] = new Vector2Int[5] { new(0, 0), new( 1, 0), new( 1, 1), new( 0,-2), new( 1,-2) };
                wallkick[new Vector2Int(3, 2)] = new Vector2Int[5] { new(0, 0), new(-1, 0), new(-1,-1), new( 0, 2), new(-1, 2) };
                wallkick[new Vector2Int(3, 0)] = new Vector2Int[5] { new(0, 0), new(-1, 0), new(-1,-1), new( 0, 2), new(-1, 2) };
                wallkick[new Vector2Int(0, 3)] = new Vector2Int[5] { new(0, 0), new( 1, 0), new( 1, 1), new( 0,-2), new( 1,-2) };
                break;
        }


        // Bcase TetrominoType.S:lock array
        blocks[0] = block1;
        blocks[1] = block2;
        blocks[2] = block3;
        blocks[3] = block4;
    }

    public TetrominoType Type { get; }

    private Block[,] shape;
    public Block[] blocks = new Block[4];
    public int size;
    public int rotation = 0;
    public int lastRotation = 0;
    private Dictionary<Vector2Int, Vector2Int[]> wallkick;
    private Vector2Int position = new Vector2Int(0, 0); // grid position

    public bool isLocked = false; // lockdown
    public bool isActive = false; // inactive tetromino is not in the map

    public delegate void OnLandedEvent(Tetromino tetromino);
    public event OnLandedEvent OnLockdown;

    public Block this[int x, int y] => shape[x, y];
    public Vector2Int[] Wallkick(int from, int to) => wallkick[new Vector2Int(from, to)];
    public Vector2Int[] Wallkick() => wallkick[new Vector2Int(lastRotation, rotation)];
    public bool IsBlock(int x, int y) => shape[x, y] != null;



    public void SetPosition(Vector2Int position)
    { 
        this.position = position;
    }
    public Vector2Int MapPosition => position;

    public Vector2Int LocalToMap(int row, int column)
    {
        return position + new Vector2Int(column, size - 1 - row);
    }

    /// <summary>
    /// Move by x right and y up, only changes tetromino data, need update on the map
    /// </summary>
    public void Move(int x, int y)
    {
        position += new Vector2Int(x, y);
    }
    /// <summary>
    /// Rotate shape of blocks, only changes tetromino data, need update on the map
    /// </summary>
    public void Rotate(bool clockwise = true)
    {
        Block[,] rotated = new Block[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (clockwise)
                    rotated[j, size - 1 - i] = shape[i, j];
                else
                    rotated[size - 1 - j, i] = shape[i, j];
            }
        }

        shape = rotated;
        lastRotation = rotation;
        rotation += clockwise ? -1 : 1;
        rotation %= 4;
        if (rotation < 0)
            rotation += 4;
    }

    /// <summary>
    /// Move the tetromino
    /// </summary>
    public void MoveTo(Vector2Int to)
    {
        // Set data of tetromino self
        SetPosition(to);

        // Move tetromino blocks
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                Block block = this[r, c];
                if (block != null)
                    block.MoveTo(LocalToMap(r, c));
            }
    }

    // On lockdown
    public void Lockdown()
    {
        isLocked = true;
        OnLockdown?.Invoke(this);

        foreach (var block in blocks)
        {
            block.Land();
        }
    }
}

public enum TetrominoType
{
    I = 0,
    O,
    T,
    J,
    L,
    S,
    Z,
    Count
}
