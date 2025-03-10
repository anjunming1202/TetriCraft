using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.U2D;

// Manage Resources of Block
// * actually Block Register + ResourcesLoader
public class BlockResourcesManager : MonoBehaviour
{
    public Material blockMaterialGeneral;
    public static Material BlockMaterialGeneral;
    public Sprite whiteSquare;
    public static Sprite WhiteSquare;

    private void Awake()
    {
        BlockMaterialGeneral = blockMaterialGeneral;
        WhiteSquare = whiteSquare;
    }

    /// <summary>
    /// Initialise registry for all blocks
    /// </summary>
    public static void RegisterBlocks()
    {
        RegisterBlock("Null Block", BlockID.Null, () => new NullBlock(), "missing_block");
        RegisterBlock("Cobblestone", BlockID.Cobblestone, () => new NormalBlock(BlockID.Cobblestone), "cobblestone");
        RegisterBlock("Dirt", BlockID.Dirt, () => new NormalBlock(BlockID.Dirt), "dirt");
        RegisterBlock("Plank", BlockID.WoodenPlanks, () => new NormalBlock(BlockID.WoodenPlanks), "oak_plank");
        RegisterBlock("Stone", BlockID.Stone, () => new NormalBlock(BlockID.Stone), "stone");

        Debug.Log($"Current registered block number: { BlockRegistry.RegistryCount }");
    }

    private static void RegisterBlock(string block_name, BlockID block_type, Func<Block> constructor, string texture_name, params string[] other_texture_names)
    {
        BlockRegistry.Register(block_name, block_type, constructor, LoadBlockTexture(texture_name), LoadBlockTexture(other_texture_names));
    }

    // Load block texture
    public static Sprite LoadBlockTexture(string texture_name)
    {
        Sprite texture = Resources.Load<Sprite>("Textures/"+texture_name);

        Debug.Assert(texture != null, $"Can't find {texture_name}!");
        Debug.Log($"Loaded texture: {texture.name}");

        return texture;
    }
    public static Sprite[] LoadBlockTexture(params string[] texture_names)
    {
        Sprite[] textures = new Sprite[texture_names.Length];
        for (int i = 0; i < texture_names.Length; i++)
        {
            textures[i] = LoadBlockTexture(texture_names[i]);
        }
        return textures;
    }
}
