using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

// Manage the rendering of the instantiated block
public class BlockRenderer : MonoBehaviour
{
    public Texture2D Texture => spriteRenderer.sprite.texture;

    // Block Texture
    [SerializeField] protected Sprite mainTexture;

    // Renderer
    protected SpriteRenderer spriteRenderer;

    // Block Reference
    private Block block;

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
