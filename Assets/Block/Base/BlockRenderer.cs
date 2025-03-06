using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

// Manage the rendering of the instantiated block
public class BlockRenderer : MonoBehaviour
{
    // Block Texture
    public Sprite texture;

    // Renderer
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void Initialise(Block block)
    {
        // Initialise texture renderer
        /*this.block = block;*/

        // Subscribe the block "change" events - render by updating block object when block data changes
        //block.OnMove += Render;
    }

    void Update()
    {
        /*RenderAnimation();*/   // if want animation, animation update here
    }


    /// <summary>
    /// Render block: set position, set texture
    /// </summary>
    private void Render(Block block)
    {
        spriteRenderer.sprite = texture;
    }
}
