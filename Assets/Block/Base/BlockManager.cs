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
        block.OnPositionChanged += UpdatePosition;      // block set position   =>  change position instantaneously
        block.OnMoved += UpdatePositionMoving;          // block on moved       =>  moving animation
        block.OnLanded += UpdatePositionLanding;        // block on landed      =>  landing animation

        block.OnDestroyed += DestroyBlock;                // block on destroyed   =>  destroy block object

        // Let block subscribe events
        blockAnimator.OnFinish += AnimationFinished;
    }

    private Block block;
    private BlockRenderer blockRenderer;
    private BlockAnimator blockAnimator;


    private void UpdatePosition(Block block)
    {
        blockAnimator.Stop();
        transform.position = block.GetWorldPosition();
    }
    private void UpdatePositionMoving(Block block)
    {
        block.isAnimating = true;
        blockAnimator.MoveAnimationOnSet(block);
    }
    private void UpdatePositionLanding(Block block)
    {
        block.isAnimating = true;
        blockAnimator.LandAnimationOnSet(block);
    }
    private void AnimationFinished()
    {
        block.isAnimating = false;
    }

    private void DestroyBlock()
    {
        GameObject.Destroy(gameObject);
    }
}
