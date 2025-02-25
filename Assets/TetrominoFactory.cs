using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Assemble blocks to a tetromino
public static class TetrominoFactory
{
    private static GameObject Tetromino;

    static TetrominoFactory()
    {
        Tetromino = GameObject.Find("Tetromino");

        if (Tetromino == null )
        {
            Debug.Assert(false, "Fail to find \"Tetromino\" object!");
        }
    }
    public static GameObject CreateTetromino(Tetromino tetromino)
    {
        foreach (Block block in tetromino.blocks)
        {
            if (block != null)
            {
                CreateBlock(block).transform.parent = Tetromino.transform;
            }
        }
        Tetromino.name = $"Tetromino {tetromino.Type}";
        return Tetromino;
    }
    public static void DeleteTetromino(Tetromino tetromino)
    {
        foreach (Block block in tetromino.blocks)
        {
            if (block != null)
            {
                //CreateBlock(block).transform.parent = Tetromino.transform;
            }
        }
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
