using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Job: instantiate tetromino (to 4 block objects) and keep instanced objects as child dynamically, if not using prefabs
public class TetrominoFactory : MonoBehaviour
{
    private static GameObject Tetromino; // parent of instantiated blocks as falling in a tetromino

    void Awake()
    {
        Tetromino = GameObject.Find("Tetromino");
    }
    public static GameObject CreateTetromino(Tetromino tetromino)
    {
        foreach (Block block in tetromino.blocks)
        {
            BlockFactory.CreateBlock(block).transform.SetParent(Tetromino.transform);
        }
        Tetromino.name = $"Tetromino {tetromino.Type}";
        return Tetromino;
    }
    /// <summary>
    /// Detach from "Tetromino" and reattach to "Blocks"
    /// </summary>
    public static void ReparentBlocks()
    {
        foreach (var obj in Tetromino.GetComponentsInChildren<Transform>())
        {
            if (obj == Tetromino.transform)
                continue;
            obj.SetParent(BlockFactory.Blocks.transform);
        }
    }
}
