using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Job: instantiate block and keep instanced objects as child dynamically, if not using prefabs
public class BlockFactory
{
    // Parent of instantiated blocks as placed in the map
    public static GameObject Blocks; 

    // Block Prefabs
    private static Dictionary<BlockType, GameObject> BlockPrefabs;

    // Block Constructor
    private static Dictionary<BlockType, Func<Block>> BlockConstructors;

    public static void Initialise()
    {
        Blocks = GameObject.Find("Blocks");

        BlockConstructors = new Dictionary<BlockType, Func<Block>>()
        {
            {BlockType.Null, () => new NullBlock() },
            {BlockType.Cobblestone, () => new NormalBlock(BlockType.Cobblestone) },
            
        };
    }
    
    /// <summary>
    /// Instantiate a block game object from block
    /// </summary>
    public static GameObject CreateBlockObject(Block block)
    {
        // Instantiate block prefab
        GameObject blockObject = GameObject.Instantiate(BlockResources.GetPrefab(block.Type));

        // Set block parent
        blockObject.transform.SetParent(Blocks.transform);

        // Initialise block object manager
        BlockObjectManager blockObjectManager = blockObject.GetComponent<BlockObjectManager>();
        blockObjectManager.Initialise(block);

        return blockObject;
    }

    /// <summary>
    /// New a block instance
    /// </summary>
    public static Block CreateBlock(BlockType type)
    {
        return BlockConstructors[type]?.Invoke();
    }

    /// <summary>
    /// New a block instance and then instantiate a block game object
    /// </summary>
    public static Block InstantiateBlock(BlockType type)
    {
        Block block = CreateBlock(type);
        CreateBlockObject(block);
        return block;
    }
}
