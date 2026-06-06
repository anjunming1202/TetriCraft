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
}
