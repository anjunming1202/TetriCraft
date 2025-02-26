using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Job: instantiate block and keep instanced objects as child
public class BlockFactory : MonoBehaviour
{
    public static GameObject Blocks;

    void Awake()
    {
        Blocks = gameObject;
    }
    
    public static GameObject CreateBlock(Block block)
    {
        GameObject gameObject = new GameObject(block.Name);
        gameObject.AddComponent<SpriteRenderer>();
        BlockRenderer blockRenderer = gameObject.AddComponent<BlockRenderer>();
        blockRenderer.Initialise(block);
        //gameObject.transform.parent = Blocks.transform;
        return gameObject;
    }
}
