using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

// Load all block prefab from assets, stored in a global static list
public static class BlockResources
{
    // Folder Path for prefabs
    private static string prefabFolderPath = "Assets/Blocks";
    // Path of block texture sprite sheet
    private static string spriteSheetFilename = "BlockTexture";

    // Loaded Block Textures
    private static Sprite[] blockSprites;   // currently not used
    public static Dictionary<string, Sprite> blockTexture;

    // Loaded Block Prefabs
    private static GameObject[] BlockPrefabs;   // currently not used
    public static Dictionary<string, GameObject> BlockPrefabsDict;



    // Load all block texture
    public static void LoadBlockTextures()
    {
        blockSprites = Resources.LoadAll<Sprite>(spriteSheetFilename);
        
        blockTexture = new Dictionary<string, Sprite>();

        foreach (Sprite sprite in blockSprites)
        {
            blockTexture[sprite.name] = sprite;
            Debug.Log($"Loaded Sprite: {sprite.name}");
        }

        Debug.Log($"Successfully Loaded {blockSprites.Length} Texture Sprite!");
    }

    // Load all block prefabs in a particular folder
    public static void LoadBlockPrefabs()
    {
        // Find all asset paths in the folder that match Prefabs
        string[] assetPaths = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });

        // Initialize the array to store loaded Prefabs
        BlockPrefabs = new GameObject[assetPaths.Length];
        BlockPrefabsDict = new Dictionary<string, GameObject>();

        // Iterate through the asset paths and load Prefabs
        for (int i = 0; i < assetPaths.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetPaths[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                BlockPrefabs[i] = prefab; // prefab list
                BlockPrefabsDict[prefab.name] = prefab; // prefab dictionary (indexed by name)
                Debug.Log($"Loaded Prefab: {prefab.name}");
            }
        }

        Debug.Log($"Successfully Loaded {BlockPrefabs.Length} Prefabs!");
    }
}
