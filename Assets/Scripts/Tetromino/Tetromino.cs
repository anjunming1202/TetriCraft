using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class Tetromino : MonoBehaviour
{
    // Blocks data
    public Block[,] shape = null;
    public Block[] blocks = new Block[4];
    public int size;

    // Tetromino type
    public TetrominoType type = TetrominoType.None;

    // GridPosition data
    public Vector2Int position;

    // Rotation data
    /// <summary>
    /// clockwise: -1
    /// </summary>
    public int rotation = 0;
    public int lastRotation = 0;

    // Wallkick table
    public Dictionary<Vector2Int, Vector2Int[]> wallkick;

    public void New(TetrominoType type, Block block1, Block block2, Block block3, Block block4)
    {
        // Initialise tetromino data
        this.type = type;
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


        // Block array
        blocks[0] = block1;
        blocks[1] = block2;
        blocks[2] = block3;
        blocks[3] = block4;

        // Reset data
        Reset();

        // Block objects reparent
        block1.transform.SetParent(transform);
        block2.transform.SetParent(transform);
        block3.transform.SetParent(transform);
        block4.transform.SetParent(transform);
    }

    public void New(Tetromino tetromino)
    {
        Block[] blocks = tetromino.blocks;
        New(tetromino.type, blocks[0], blocks[1], blocks[2], blocks[3]);
    }

    public virtual void Reset()
    {
        position = new Vector2Int(0, 0);
        rotation = 0;
        lastRotation = 0;
    }

    public void RotateShape(bool clockwise = true)
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

    public Vector2Int LocalToMap(int row, int column)
    {
        return position + new Vector2Int(column, size - 1 - row);
    }

    public Vector3 GetWorldPosition()
    {
        return MapBoundaryData.MapToWorld((Vector2)position + Vector2.one * ((float)size / 2));
    }
}

public enum TetrominoType
{
    None = -1,
    I = 0,
    O,
    T,
    J,
    L,
    S,
    Z,
    Count
}

