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
                BlockFactory.CreateBlock(block).transform.SetParent(Tetromino.transform);
            }
        }
        Tetromino.name = $"Tetromino {tetromino.Type}";
        return Tetromino;
    }
    /// <summary>
    /// Detach from "Tetromino" and reattach to "Blocks"
    /// </summary>
    public static void ReparentAsBlocks()
    {
        foreach (var block_object in Tetromino.GetComponentsInChildren<Transform>())
        {
            block_object.SetParent(BlockFactory.Blocks.transform);
        }
    }
}
