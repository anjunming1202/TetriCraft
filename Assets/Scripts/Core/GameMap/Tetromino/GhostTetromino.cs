using System;
using UnityEngine;

public class GhostTetromino : DummyTetromino
{
    [SerializeField] private Block ghostBlock;

    public void CreateGhostBlocks()
    {
        Debug.Assert(blocks[0] == null);

        TetrominoGenerator.NewTetromino(this, TetrominoType.I, ghostBlock);
    }

    public void Shadow(Tetromino tetrominoShape)
    {
        //Vector2Int position = this.position;
        //this.position = position;

        type = tetrominoShape.type;
        size = tetrominoShape.size;
        shape = new Block[size, size];
        int blockCount = 0;

        for (int r = 0; r < tetrominoShape.size; r++)
            for (int c = 0; c < tetrominoShape.size; c++)
            {
                if (tetrominoShape.shape[r, c] is Block shadowedBlock && shadowedBlock != null)
                {
                    GhostBlock block = blocks[blockCount] as GhostBlock;
                    block.Shadow(shadowedBlock);
                    shape[r, c] = block;
                    block.OnTriggerAppearanceChanged();
                    blockCount++;
                }
            }
    }
}
