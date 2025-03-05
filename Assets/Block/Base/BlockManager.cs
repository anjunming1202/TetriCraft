using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    private void Awake()
    {

    }

    public void Initialise(Block block)
    {
        // Initialise block renderer
        blockRenderer = GetComponent<BlockRenderer>();
        blockRenderer.Initialise(block);

        // Initialise block animator
        blockAnimator = GetComponent<BlockAnimator>();
        blockAnimator.Initialise(block);

        // Set initial spawn position (avoid being seen at strange position)
        gameObject.transform.position = block.GetWorldPosition();

        // Subscribe block destroy event (automatic destroy object)
        block.OnDestroy += DestroyBlock;
        //
        blockAnimator.OnFinish += block.StopMoving;
    }

    private Block Block;
    private BlockRenderer blockRenderer;
    private BlockAnimator blockAnimator;



    private void DestroyBlock()
    {
        GameObject.Destroy(gameObject);
    }
}
