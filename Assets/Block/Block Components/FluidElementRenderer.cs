using UnityEngine;

public class FluidElementRenderer : MonoBehaviour
{
    private FluidElement fluidElement;
    private SpriteRenderer spriteRenderer;

    private Material material;
    private MaterialPropertyBlock props;

    void Awake()
    {
        fluidElement = GetComponent<FluidElement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        material = GetComponent<Material>();
        props = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Render fluidBlock: set position, set texture
    /// </summary>
    public void Render(Sprite fluidTexture)
    {
        // Set sprite
        spriteRenderer.sprite = fluidTexture;

        // Set material
        spriteRenderer.GetPropertyBlock(props);
        props.SetFloat("_UpperLevel", fluidElement.upperLevel);
        props.SetFloat("_LowerLevel", fluidElement.lowerLevel);
        spriteRenderer.SetPropertyBlock(props);

        // Set transform size
        Vector2 currentSize = spriteRenderer.bounds.size;
        Vector2 targetSize = new Vector2(1f, fluidElement.upperLevel - fluidElement.lowerLevel);
        transform.localScale = transform.localScale * targetSize / currentSize;

        // Set transform position
        float midLevel = (fluidElement.upperLevel + fluidElement.lowerLevel) / 2;
        transform.localPosition = new Vector3(0, -0.5f + midLevel);
    }
}
