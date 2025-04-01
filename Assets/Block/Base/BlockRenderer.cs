using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

// Manage the rendering of the instantiated block
public class BlockRenderer : MonoBehaviour
{
    // Block Reference
    private Block block;

    // Block Texture
    public Sprite mainTexture;
    public Texture2D Texture => spriteRenderer.sprite.texture;

    // Renderer
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        block = GetComponent<Block>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        Render(block);
    }

    void Update()
    {
        
    }


    /// <summary>
    /// Render block: set position, set texture
    /// </summary>
    private void Render(Block block)
    {
        spriteRenderer.sprite = mainTexture;
    }
}
