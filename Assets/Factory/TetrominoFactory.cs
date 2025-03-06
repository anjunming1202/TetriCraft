using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Job: instantiate tetromino (to 4 block objects) and keep instanced objects as child dynamically, if not using prefabs
public class TetrominoFactory
{
    public static void Initialise()
    {

    }

    public static void InstantiateTetromino(Tetromino tetromino, Transform parent)
    {
        foreach (Block block in tetromino.blocks)
        {
            BlockFactory.InstantiateBlock(block, parent);
        }
    }

    /// <summary>
    /// Detach from "Tetromino" and reattach to "Blocks"
    /// </summary>
    public static void ReparentBlocks(Transform from, Transform to)
    {
        foreach (var obj in from.GetComponentsInChildren<Transform>())
        {
            if (obj == from.transform)
                continue;
            obj.SetParent(to.transform);
        }
    }
}
