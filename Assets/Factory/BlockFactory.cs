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
    }
    
    public static GameObject CreateBlock(Block block)
    {
        // Instantiate block prefab
        GameObject blockObject = GameObject.Instantiate(BlockResources.GetPrefab(block.Type));

        // Initialise block object manager
        BlockObjectManager blockObjectManager = blockObject.GetComponent<BlockObjectManager>();
        blockObjectManager.Initialise(block);

        return blockObject;
    }
}
