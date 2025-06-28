/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidRenderer : MonoBehaviour
{
    [SerializeField] private Sprite stillTexture;
    [SerializeField] private Sprite flowingTexture;
    [SerializeField] private Material stillMaterial;
    [SerializeField] private Material flowingMaterial;

    private FluidBlock fluidBlock;
    private SpriteRenderer spriteRenderer;

    private Material material;
    private MaterialPropertyBlock props;

    void Awake()
    {
        fluidBlock = GetComponent<FluidBlock>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        material = GetComponent<Material>();
        props = new MaterialPropertyBlock();
        Render();
    }

    void Update()
    {
        Render();
    }




    /// <summary>
    /// Render fluidBlock: set position, set texture
    /// </summary>
    public void Render()
    {
        // Set sprite
        if (fluidBlock.IsFlowingDown)
        {
            spriteRenderer.sprite = flowingTexture;
            spriteRenderer.material = flowingMaterial;
        }
        else
        {
            spriteRenderer.sprite = stillTexture;
            spriteRenderer.material = stillMaterial;
        }

        // Set material
        spriteRenderer.GetPropertyBlock(props);
        props.SetColor("_Color", spriteRenderer.color); //
        props.SetFloat("_UpperLevel", fluidBlock.upperLevel);
        props.SetFloat("_LowerLevel", fluidBlock.lowerLevel);
        spriteRenderer.SetPropertyBlock(props);

        // Set transform size
        Vector2 currentSize = spriteRenderer.bounds.size;
        Vector2 targetSize = new Vector2(1f, fluidBlock.height);
        transform.localScale = transform.localScale * targetSize / currentSize;

        // Set transform position
        float gridX = fluidBlock.Position.x;
        float gridY = fluidBlock.Position.y - 0.5f + fluidBlock.midLevel;
        transform.position = MapBoundaryData.MapToWorld(new Vector2(gridX, gridY));
    }
}
*/