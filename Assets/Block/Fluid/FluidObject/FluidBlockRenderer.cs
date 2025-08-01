using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidBlockRenderer : BlockRenderer
{
    private void Awake()
    {
        base.Awake();
    }

    protected override void Render(Block block)
    {
        // Set transform size
        Vector2 currentSize = spriteRenderer.bounds.size;
        Vector2 targetSize = new Vector2(1, 1);
        transform.localScale = transform.localScale * targetSize / currentSize;

        base.Render(block);
    }
}
