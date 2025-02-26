using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Job: instantiate tetromino (to 4 block objects) and keep instanced objects as child
public class TetrominoFactory : MonoBehaviour
{
    private static GameObject Tetromino;

    void Awake()
    {
        Tetromino = gameObject;
    }
    public static GameObject CreateTetromino(Tetromino tetromino)
    {
        foreach (Block block in tetromino.blocks)
        {
            if (block != null)
            {
                BlockFactory.CreateBlock(block).transform.parent = Tetromino.transform;
            }
        }
        Tetromino.name = $"Tetromino {tetromino.Type}";
        return Tetromino;
    }
    public static void DeleteTetromino(Tetromino tetromino)
    {
        foreach (var block_object in Tetromino.GetComponentsInChildren<Transform>())
        {
            block_object.parent = BlockFactory.Blocks.transform;
        }
    }
}
