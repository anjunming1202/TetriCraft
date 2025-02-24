using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Assemble blocks to a tetromino; Spawn (Place) the tetromino to the spawn point
public static class TetrominoFactory
{
    /*public GameObject CreateTetromino(TetrominoType type) { return CreateTetromino(type, new NullBlock()); }
    public GameObject CreateTetromino(TetrominoType type, Block block) { return CreateTetromino(type, block, block, block, block); }
    public GameObject CreateTetromino(TetrominoType type, Block block1, Block block2, Block block3, Block block4)
    {
        GameObject tetrominoObject = new GameObject();
        blockFactory.CreateBlock(block1).transform.parent = tetrominoObject.transform;
        blockFactory.CreateBlock(block2).transform.parent = tetrominoObject.transform;
        blockFactory.CreateBlock(block3).transform.parent = tetrominoObject.transform;
        blockFactory.CreateBlock(block4).transform.parent = tetrominoObject.transform;
        return tetrominoObject;
    }*/
    public static GameObject CreateTetromino(Tetromino tetromino)
    {
        GameObject tetrominoObject = new GameObject();
        foreach (Block block in tetromino.blocks)
        {
            if (block != null)
            {
                CreateBlock(block).transform.parent = tetrominoObject.transform;
            }
        }
        tetrominoObject.name = $"Tetromino {tetromino.Type}";
        tetrominoObject.transform.parent = GameObject.Find("Blocks").transform;
        return tetrominoObject;
    }
    public static GameObject CreateBlock(Block block)
    {
        GameObject gameObject = new GameObject();
        gameObject.name = block.Name;
        gameObject.AddComponent<SpriteRenderer>();
        BlockRenderer blockRenderer = gameObject.AddComponent<BlockRenderer>();
        blockRenderer.Initialise(block);
        return gameObject;
    }
}
