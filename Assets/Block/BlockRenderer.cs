using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockRenderer : MonoBehaviour
{
    // Reference of block data
    public Block block;             // Use event-subscribe model, not having block reference

    // Renderer
    public SpriteRenderer spriteRenderer;


    void Awake()
    {

    }
    public void Initialise(Block block)
    {
        // Initialise texture renderer
        this.block = block;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = BlockResources.blockTexture[block.Name];
        spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; // only seen in the map region

        /*// Subscribe the OnChange event - update block data when block data changes
        this.block.OnChanged += UpdateBlockData;*/
    }

    void Update()
    {
        Render();
    }

    private void Render()
    {
        if (block.isFalling)
        {
            Vector3 targetPosition = MapBoundaryData.GridToWorld(block.position);
            transform.position = targetPosition;
        }
    }



    ////////////////////////////////
    /*// When block data changes, update block data by subscribing OnChange event
    private void UpdateBlockData(Block block)
    {
        this.block = block;
    }*/
}
