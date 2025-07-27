using UnityEngine;

public class TetrominoGenerator
{
    public static void NewRandomTetromino(Tetromino tetromino)
    {
        // Random tetromino type
        TetrominoType tetroType = TetrominoType.I /*(TetrominoType)UnityEngine.Random.Range(0, (int)TetrominoType.Count)*/;

        // Random blocks type
        BlockID blockType = BlockRandomSelector.GetRandomBlockID();

        NewTetromino(tetromino, tetroType, blockType);
    }

    public static void Clone(Tetromino src, Tetromino dst)
    {
        Block[] blocks = new Block[4];
        for (int i = 0; i < 4; i++)
        {
            blocks[i] = BlockSpawner.NewBlock(src.blocks[i].ID);
        }

        dst.New(src.type, blocks[0], blocks[1], blocks[2], blocks[3]);
    }

    private static void NewTetromino(Tetromino tetromino, TetrominoType tetroType, BlockID blockType)
    {
        // For intrinsic tetromino (same four blocks)
        Block[] blocks = new Block[4];
        for (int i = 0; i < 4; i++)
        {
            blocks[i] = BlockSpawner.NewBlock(blockType);
        }

        // New a tetromino
        tetromino.New(tetroType, blocks[0], blocks[1], blocks[2], blocks[3]);
    }
}
