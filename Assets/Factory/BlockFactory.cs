using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Job: instantiate block and keep instanced objects as child dynamically, if not using prefabs
public class BlockFactory : MonoBehaviour
{    
    public static GameObject Blocks; // parent of instantiated blocks as placed in the map

    void Awake()
    {
        Blocks = GameObject.Find("Blocks");
    }
    
    public static GameObject CreateBlock(Block block)
    {
        GameObject blockObject = new GameObject(block.Name);

        // Add sprite renderer
        blockObject.AddComponent<SpriteRenderer>();

        // Add block renderer
        BlockRenderer blockRenderer = blockObject.AddComponent<BlockRenderer>();
        blockRenderer.Initialise(block);
        
        // Add block animator
        BlockAnimator blockAnimator = blockObject.AddComponent<BlockAnimator>();
        blockAnimator.Initialise(block);

        // Initial spawn position (avoid being seen at strange position)
        blockObject.transform.position = MapBoundaryData.GridToWorld(block.position);

        return blockObject;
    }

    public static void DestroyBlock(Block block)
    {

    }
}
