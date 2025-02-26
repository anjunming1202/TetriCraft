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
        GameObject blockObject = new GameObject(block.Name);

        // Add sprite renderer
        blockObject.AddComponent<SpriteRenderer>();
        // Add block renderer
        BlockRenderer blockRenderer = blockObject.AddComponent<BlockRenderer>();
        blockRenderer.Initialise(block);
        // Initial spawn position (avoid being seen at strange position)
        blockObject.transform.position = MapBoundaryData.GridToWorld(block.position);

        return blockObject;
    }

    public static void DestroyBlock(Block block)
    {

    }
}
