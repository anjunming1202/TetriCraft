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

        // Add block object manager
        BlockManager blockObjectManager = blockObject.AddComponent<BlockManager>();
        blockObjectManager.Initialise(block);

        return blockObject;
    }
}
