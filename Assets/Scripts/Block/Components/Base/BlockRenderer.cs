using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

// Manage the rendering of the instantiated block
public class BlockRenderer : MonoBehaviour
{
    public Texture2D Texture => spriteRenderer.sprite.texture;

    public virtual void ChangeState(int state)
    {
        mainTexture = textures[state];
    }

    // Block Texture
    [SerializeField] protected Sprite[] textures;
    protected Sprite mainTexture;

    // Renderer
    protected SpriteRenderer spriteRenderer;

    // Block Reference
    protected Block block;

    protected void Awake()
    {
        block = GetComponent<Block>();
        block.OnStateChanged += Render;

        spriteRenderer = GetComponent<SpriteRenderer>();
        mainTexture = textures[0];

        Render(block);
    }

    protected void Update()
    {
        
    }


    /// <summary>
    /// Render block: set position, set texture
    /// </summary>
    protected virtual void Render(Block block)
    {
        spriteRenderer.sprite = mainTexture;
    }
}
