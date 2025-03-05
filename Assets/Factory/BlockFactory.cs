using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Job: instantiate block and keep instanced objects as child dynamically, if not using prefabs
public static class BlockFactory
{
    // Parent of instantiated blocks as placed in the map
    public static GameObject Blocks; 

    public static void Initialise()
    {
        Blocks = GameObject.Find("Blocks");
    }
    
    /// <summary>
    /// Instantiate a block game object from block
    /// </summary>
    public static GameObject CreateBlockObject(Block block)
    {
        // Instantiate block prefab
        GameObject blockObject = Instantiate(block.Type);

        // Set block parent
        blockObject.transform.SetParent(Blocks.transform);

        // Initialise block manager
        BlockManager blockManager = blockObject.GetComponent<BlockManager>();
        blockManager.Initialise(block);

        return blockObject;
    }

    /// <summary>
    /// Instantiate an empty block template (exactly like instantiate a prefab!)
    /// </summary>
    private static GameObject Instantiate(BlockID type)
    {
        GameObject block = new GameObject();
        block.name = BlockRegistry.GetMetadata(type).Name;

        // Add component SpriteRenderer
        SpriteRenderer spriteRenderer = block.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = BlockRegistry.GetMetadata(type).DefaultTexture;
        spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; // only seen in the map region

        // Add component BlockManager
        BlockManager blockManager = block.AddComponent<BlockManager>();

        // Add component BlockRenderer
        BlockRenderer blockRenderer = block.AddComponent<BlockRenderer>();
        blockRenderer.texture = BlockRegistry.GetMetadata(type).DefaultTexture;

        // Add component BlockAnimator
        block.AddComponent<BlockAnimator>();

        return block;
    }

    /// <summary>
    /// New a block instance
    /// </summary>
    public static Block NewBlock(BlockID type)
    {
        return BlockRegistry.GetMetadata(type).Constructor?.Invoke();
    }
}
