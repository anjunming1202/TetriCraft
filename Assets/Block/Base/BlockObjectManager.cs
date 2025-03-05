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
<<<<<<<< HEAD:Assets/Block/BlockManager.cs
        // Block reference
        this.block = block;

        // Add sprite renderer
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();

        // Add block renderer
        blockRenderer = gameObject.AddComponent<BlockRenderer>();
========
        // Initialise block renderer
        blockRenderer = GetComponent<BlockRenderer>();
>>>>>>>> 921ced58143d5fabea2631d34a06b3b254cd650d:Assets/Block/Base/BlockObjectManager.cs
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

<<<<<<<< HEAD:Assets/Block/BlockManager.cs
    private Block block;
========
    private Block Block;
>>>>>>>> 921ced58143d5fabea2631d34a06b3b254cd650d:Assets/Block/Base/BlockObjectManager.cs
    private BlockRenderer blockRenderer;
    private BlockAnimator blockAnimator;



    private void DestroyBlock()
    {
        GameObject.Destroy(gameObject);
    }
}
