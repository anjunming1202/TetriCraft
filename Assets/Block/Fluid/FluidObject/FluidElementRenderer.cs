using UnityEngine;

public class FluidElementRenderer : MonoBehaviour
{
    private FluidElement fluidElement;
    private SpriteRenderer spriteRenderer;

    private Material material;
    private MaterialPropertyBlock props;

    [SerializeField] private Sprite stillTexture;
    [SerializeField] private Sprite flowingTexture;
    [SerializeField] private Material stillMaterial;
    [SerializeField] private Material flowingMaterial;

    void Awake()
    {
        fluidElement = GetComponent<FluidElement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        material = GetComponent<Material>();
        props = new MaterialPropertyBlock();

        spriteRenderer.enabled = false;
    }

    private void Start()
    {
        Render();
        spriteRenderer.enabled = true;
    }

    private void Update()
    {
        Render();
    }

    /// <summary>
    /// Render fluidBlock: set position, set texture
    /// </summary>
    public void Render()
    {
        // Set sprite
        if (fluidElement.isFalling)
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
        props.SetFloat("_UpperLevel", fluidElement.upperLevel);
        props.SetFloat("_LowerLevel", fluidElement.lowerLevel);
        spriteRenderer.SetPropertyBlock(props);

        // Set transform size
        Vector2 currentSize = spriteRenderer.bounds.size;
        Vector2 targetSize = new Vector2(fluidElement.width, fluidElement.height);
        transform.localScale = transform.localScale * targetSize / currentSize;

        // Set transform position
        transform.position = MapBoundaryData.MapToWorld(fluidElement.mapPosition);
    }
}
