using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

// Manage the rendering of the instantiated block
public class BlockRenderer : MonoBehaviour
{
    public Texture2D Texture => spriteRenderer.sprite.texture;
    public Sprite DefaultSprite;

    public virtual void ChangeState(int state)
    {
        mainTexture = textures[state];
    }

    // Block Texture
    [SerializeField] protected Sprite[] textures;
    protected Sprite mainTexture;

    // Material
    protected MaterialPropertyBlock props;

    // Renderer
    protected SpriteRenderer spriteRenderer;

    // Block Reference
    protected Block block;

    protected virtual void Awake()
    {
        block = GetComponent<Block>();
        block.OnStateChanged += Render;

        spriteRenderer = GetComponent<SpriteRenderer>();
        mainTexture = textures.Length > 0 ? textures[0] : null;

        props = new MaterialPropertyBlock();

        Render(block);
    }


    /// <summary>
    /// Render block: set position, set texture
    /// </summary>
    protected virtual void Render(Block block)
    {
        spriteRenderer.sprite = mainTexture;

        // material rendering
        spriteRenderer.GetPropertyBlock(props);
        props.SetColor("_Color", spriteRenderer.color);
        props.SetFloat("_Rotation", block.Rotation);
        spriteRenderer.SetPropertyBlock(props);
    }
}
