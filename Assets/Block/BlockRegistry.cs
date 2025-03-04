using System.Collections.Generic;
using System;
using UnityEngine.U2D;

public class BlockRegistry
{
    public class BlockMetadata
    {
        public string Name;
        public BlockType Type;
        public Func<Block> Constructor;
        public SpriteAtlas SpriteAtlas;
    }

    // Look-up dictionary for block metadata
    private static Dictionary<BlockType, BlockMetadata> registry = new Dictionary<BlockType, BlockMetadata>();

    // Register block
    public static void Register(BlockType type, string name, Func<Block> constructor)
    {
        if (!registry.ContainsKey(type))
        {
            registry[type] = new BlockMetadata
            {
                Name = name,
                Type = type,
                Constructor = constructor
            };
        }
    }

    // Get block metadata
    public static BlockMetadata GetMetadata(BlockType type)
    {
        return registry.TryGetValue(type, out var metadata) ? metadata : null;
    }

    // Create new block instance
    public static Block CreateBlock(BlockType type)
    {
        var metadata = GetMetadata(type);
        return metadata?.Constructor.Invoke();
    }
}
