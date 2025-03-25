using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class Tetromino
{
    // Blocks data
    public Block[,] shape;
    public Block[] blocks = new Block[4];
    public int size;

    // Tetromino type
    public TetrominoType Type { get; }

    // Position data (in map)
    public Vector2Int position;

    // Rotation data
    public int rotation = 0;
    public int lastRotation = 0;

    // Wallkick table
    public Dictionary<Vector2Int, Vector2Int[]> wallkick;

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
                wallkick[new Vector2Int(0, 1)] = new Vector2Int[5] { new(0, 0), new(-2, 0), new(1, 0), new(-2, -1), new(1, 2) };
                wallkick[new Vector2Int(1, 0)] = new Vector2Int[5] { new(0, 0), new(2, 0), new(-1, 0), new(2, 1), new(-1, -2) };
                wallkick[new Vector2Int(1, 2)] = new Vector2Int[5] { new(0, 0), new(-1, 0), new(2, 0), new(-1, 2), new(2, -1) };
                wallkick[new Vector2Int(2, 1)] = new Vector2Int[5] { new(0, 0), new(1, 0), new(-2, 0), new(1, -2), new(-2, 1) };
                wallkick[new Vector2Int(2, 3)] = new Vector2Int[5] { new(0, 0), new(2, 0), new(-1, 0), new(2, 1), new(-1, -2) };
                wallkick[new Vector2Int(3, 2)] = new Vector2Int[5] { new(0, 0), new(-2, 0), new(1, 0), new(-2, -1), new(1, 2) };
                wallkick[new Vector2Int(3, 0)] = new Vector2Int[5] { new(0, 0), new(1, 0), new(-2, 0), new(1, -2), new(-2, 1) };
                wallkick[new Vector2Int(0, 3)] = new Vector2Int[5] { new(0, 0), new(-1, 0), new(2, 0), new(-1, 2), new(2, -1) };
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
                wallkick[new Vector2Int(0, 1)] = new Vector2Int[5] { new(0, 0), new(-1, 0), new(-1, 1), new(0, -2), new(-1, -2) };
                wallkick[new Vector2Int(1, 0)] = new Vector2Int[5] { new(0, 0), new(1, 0), new(1, -1), new(0, 2), new(1, 2) };
                wallkick[new Vector2Int(1, 2)] = new Vector2Int[5] { new(0, 0), new(1, 0), new(1, -1), new(0, 2), new(1, 2) };
                wallkick[new Vector2Int(2, 1)] = new Vector2Int[5] { new(0, 0), new(-1, 0), new(-1, 1), new(0, -2), new(-1, -2) };
                wallkick[new Vector2Int(2, 3)] = new Vector2Int[5] { new(0, 0), new(1, 0), new(1, 1), new(0, -2), new(1, -2) };
                wallkick[new Vector2Int(3, 2)] = new Vector2Int[5] { new(0, 0), new(-1, 0), new(-1, -1), new(0, 2), new(-1, 2) };
                wallkick[new Vector2Int(3, 0)] = new Vector2Int[5] { new(0, 0), new(-1, 0), new(-1, -1), new(0, 2), new(-1, 2) };
                wallkick[new Vector2Int(0, 3)] = new Vector2Int[5] { new(0, 0), new(1, 0), new(1, 1), new(0, -2), new(1, -2) };
                break;
        }


        // Bcase TetrominoType.S:lock array
        blocks[0] = block1;
        blocks[1] = block2;
        blocks[2] = block3;
        blocks[3] = block4;
    }

    public Vector2Int LocalToMap(int row, int column)
    {
        return position + new Vector2Int(column, size - 1 - row);
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

