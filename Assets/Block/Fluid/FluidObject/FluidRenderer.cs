using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidRenderer : MonoBehaviour
{
    [SerializeField] private Sprite stillTexture;

    private FluidBlock fluidBlock;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        fluidBlock = GetComponent<FluidBlock>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        Render();

        fluidBlock.OnUpdated += Render;
    }

    void Update()
    {
        Render();
    }


    /// <summary>
    /// Render fluidBlock: set position, set texture
    /// </summary>
    private void Render()
    {
        foreach (FluidElementRenderer renderer in GetComponentsInChildren<FluidElementRenderer>())
        {
            if (!fluidBlock.isFlowing)
                renderer.Render(stillTexture);
        }
    }
}
