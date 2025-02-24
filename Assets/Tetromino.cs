// Four blocks are one tetromino
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

// A tetromino stores 4 blocks, when falling
[Serializable]
public class Tetromino
{
    public TetrominoType Type { get; }

    public Block[,] blocks;
    public Vector2Int position = new Vector2Int(0, 0); // grid position
    public int size;

    public Tetromino(TetrominoType type) : this(type, new NullBlock(), new NullBlock(), new NullBlock(), new NullBlock()) { }
    // public Tetromino(TetrominoType type, BlockType blockType) : this(type, block, block, block, block) { }
    public Tetromino(TetrominoType type, Block block1, Block block2, Block block3, Block block4)
    {
        // Initialise tetromino data
        Type = type;
        switch (type)
        {
            case TetrominoType.I:
                blocks = new Block[4, 4] {
                { null, null, null, null },
                { block1, block2, block3, block4 },
                { null, null, null, null },
                { null, null, null, null } };
                size = 4;
                break;
            case TetrominoType.O:
                blocks = new Block[4, 4] {
                { null, null, null, null },
                { null, block1, block2, null },
                { null, block3, block4, null },
                { null, null, null, null } };
                size = 4;
                break;
            case TetrominoType.T:
                blocks = new Block[3, 3] {
                { null, block1, null },
                { block2, block3, block4 },
                { null, null, null } };
                size = 3;
                break;
            case TetrominoType.J:
                blocks = new Block[3, 3] {
                { block1, null, null },
                { block2, block3, block4 },
                { null, null, null } };
                size = 3;
                break;
            case TetrominoType.L:
                blocks = new Block[3, 3] {
                { null, null, block1 },
                { block2, block3, block4 },
                { null, null, null } };
                size = 3;
                break;
            case TetrominoType.S:
                blocks = new Block[3, 3] {
                { null, block1, block2 },
                { block3, block4, null },
                { null, null, null } };
                size = 3;
                break;
            case TetrominoType.Z:
                blocks = new Block[3, 3] {
                { block1, block2, null },
                { null, block3, block4 },
                { null, null, null } };
                size = 3;
                break;
        }

        // Initialise block data as in a tetromino
        foreach (var block in new Block[] { block1, block2, block3, block4 })
        {
            block.isMoving = true;
        }
    }
    
    public Block this[int x, int y] => blocks[x, y];
    public bool IsBlock(int x, int y) => blocks[x, y] != null;

    public void GoTo(Grid grid, int x, int y)
    {
        GoTo(grid, new Vector2Int(x, y));
    }
    public void GoTo(Grid grid, Vector2Int to)
    {
        for (int j = 0; j < size; j++) // column
            for (int i = 0; i < size; i++) // row
            {
                Block block = blocks[i, j];
                if (block != null)
                    block.GoTo(grid, to + new Vector2Int(j, size - 1 - i));
            }
    }
    public void Fall()
    {

    }
    public void Left()
    {

    }
    public void Right()
    {

    }
    public void Rotate(bool isclockwise = true)
    {

    }
}

public enum TetrominoType
{
    I,
    O,
    T,
    J,
    L,
    S,
    Z
}
