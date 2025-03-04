using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockObjectManager : MonoBehaviour
{
    public void Initialise(Block block)
    {
        // Add sprite renderer
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();

        // Add block renderer
        blockRenderer = gameObject.AddComponent<BlockRenderer>();
        blockRenderer.Initialise(block);

        // Add block animator
        blockAnimator = gameObject.AddComponent<BlockAnimator>();
        blockAnimator.Initialise(block);

        // Set initial spawn position (avoid being seen at strange position)
        gameObject.transform.position = block.GetWorldPosition();

        // Subscribe block destroy event (automatic destroy object)
        block.OnDestroy += DestroyBlock;
        //
        blockAnimator.OnFinish += block.StopMoving;
    }

    private BlockRenderer blockRenderer;
    private BlockAnimator blockAnimator;



    private void DestroyBlock()
    {
        GameObject.Destroy(gameObject);
    }
}
