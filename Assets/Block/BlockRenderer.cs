using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

// Manage the rendering of the instantiated block
public class BlockRenderer : MonoBehaviour
{
    // Reference of block
    /*public Block block;*/

    // Renderer
    private SpriteRenderer spriteRenderer;

    void Awake()
    {

    }
    public void Initialise(Block block)
    {
        // Initialise texture renderer
        /*this.block = block;*/
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = BlockResources.blockTexture[block.Name];
        spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; // only seen in the map region

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
        spriteRenderer.sprite = block.texture;
    }
}
