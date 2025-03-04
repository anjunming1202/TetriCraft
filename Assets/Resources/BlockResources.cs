using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Load all block prefab from assets, stored in a global static list
public static class BlockResources
{
    // Path of block texture sprite sheet
    private static string spriteSheetFilename = "BlockTexture";

    // Loaded Block Textures
    private static Sprite[] blockSprites;   // currently not used
    public static Dictionary<string, Sprite> blockTexture;



    // Load all block texture
    public static void LoadBlockTextures()
    {
        blockSprites = Resources.LoadAll<Sprite>(spriteSheetFilename);
        
        blockTexture = new Dictionary<string, Sprite>();
        
        foreach (Sprite sprite in blockSprites)
        {
            blockTexture[sprite.name] = sprite;
            //Debug.Log($"Loaded Sprite: {sprite.name}");
        }

        Debug.Log($"Successfully Loaded {blockSprites.Length} Texture Sprite!");
    }

    // Register for all blocks
    public static void RegisterBlocks()
    {

    }
}
