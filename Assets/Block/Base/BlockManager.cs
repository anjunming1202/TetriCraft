using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    public void Initialise(Block block)
    {
        // Initialise block and components
        this.block = block;
        blockRenderer = GetComponent<BlockRenderer>();
        blockAnimator = GetComponent<BlockAnimator>();

        // Subscribe block events
        // animator:
        block.OnPositionChanged += UpdatePosition;      // block set position   =>  change position instantaneously
        block.OnMoved += UpdatePositionMoving;          // block on moved       =>  moving animation
        //block.OnPlaced += UpdatePositionLanding;        // block on landed      =>  landing animation
        // renderer:
        block.OnPlaced += BlockOnPlaced;                // block on placed      =>  effect
        // manager:
        block.OnDestroyed += DestroyBlock;                // block on destroyed   =>  destroy block object

        // Subscribe animator callback
        blockAnimator.OnFinish += AnimationFinished;
    }

    private Block block;
    private BlockRenderer blockRenderer;
    private BlockAnimator blockAnimator;



    // Position change:
    private void UpdatePosition(Block block)
    {
        blockAnimator.Stop();
        transform.position = block.GetWorldPosition();
    }
    private void UpdatePositionMoving(Block block)
    {
        if (block.GetWorldPosition() != transform.position)
        {
            block.isAnimating = true;
            blockAnimator.MoveAnimationOnSet(block);
        }
    }
    private void UpdatePositionLanding(Block block)
    {
        if (block.GetWorldPosition() != transform.position)
        {
            block.isAnimating = true;
            blockAnimator.LandAnimationOnSet(block);
        }
    }
    private void AnimationFinished()
    {
        block.isAnimating = false;
    }

    private void BlockOnPlaced(Block block)
    {
        blockRenderer.FlashOnSet(block);
    }


    // Lifecycle
    private void DestroyBlock()
    {
        GameObject.Destroy(gameObject);
    }
}
