/*using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Static registry of resources of block identification and corresponding data
/// </summary>
public static class BlockRegistry
{
    public class BlockMetadata
    {
        public string Name;
        public BlockID Type;
        public Func<Block> Constructor;
        public Sprite DefaultTexture;
        public Dictionary<string, Sprite> TexturePackage;
    }

    // Register block
    public static void Register(string name, BlockID type, Func<Block> constructor, Sprite texture, params Sprite[] other_textures)
    {
        if (!registry.ContainsKey(type))
        {
            registry[type] = new BlockMetadata
            {
                Name = name,
                Type = type,
                Constructor = constructor,
                DefaultTexture = texture,
                TexturePackage = new Dictionary<string, Sprite>
                {
                    {texture.name, texture}
                }
            };
            foreach (Sprite other_texture in other_textures)
            {
                if (registry[type].TexturePackage.ContainsKey(other_texture.name))
                {
                    Debug.LogError($"Block {name} already having {other_texture.name} texture!");
                }
                registry[type].TexturePackage[other_texture.name] = other_texture;
            }
        }
    }

    // Get block metadata
    public static BlockMetadata GetMetadata(BlockID type)
    {
        registry.TryGetValue(type, out var metadata);

        Debug.Assert(metadata != null, $"{type} block has not been registered!");

        return metadata;
    }

    // Debug
    // Get registered block count
    public static int RegistryCount => registry.Count;


    
    // Look-up dictionary for block metadata
    private static Dictionary<BlockID, BlockMetadata> registry = new Dictionary<BlockID, BlockMetadata>();
}
*/