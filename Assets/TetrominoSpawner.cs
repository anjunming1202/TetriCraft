using UnityEngine;

public class TetrominoSpawner : MonoBehaviour
{ 
    static public TetrominoSpawner Instance;

    private void Awake()
    {
        Instance = this;
    }

    public Tetromino NewRandomTetromino()
    {
        // Random tetromino type
        TetrominoType tetroType = (TetrominoType)UnityEngine.Random.Range(0, (int)TetrominoType.Count);

        // Random blocks type
        BlockID blockType = BlockRandomSelector.GetRandomType();

        return NewTetromino(tetroType, blockType);
    }

    private Tetromino NewTetromino(TetrominoType tetroType, BlockID blockType)
    {
        // For intrinsic tetromino (same four blocks)
        Block[] blocks = new Block[4];
        for (int i = 0; i < 4; i++)
        {
            blocks[i] = BlockSpawner.Instance.NewBlock(blockType);
            blocks[i].transform.SetParent(this.transform);
        }

        // New a tetromino
        return new Tetromino(tetroType, blocks[0], blocks[1], blocks[2], blocks[3]);
    }
}
