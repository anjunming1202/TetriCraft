using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

public class BlockRenderer : MonoBehaviour
{
    // Reference of block data
    /*public Block block;   */          // Use event-subscribe model, not having block reference

    // Renderer
    public SpriteRenderer spriteRenderer;


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
        block.OnChanged += Render;
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
        Vector3 targetPosition = MapBoundaryData.GridToWorld(block.position);
        transform.position = targetPosition;
        spriteRenderer.sprite = block.texture;
    }
}
