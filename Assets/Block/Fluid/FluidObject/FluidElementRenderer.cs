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
    }

    private void Start()
    {
        Render();
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
        if (fluidElement.isFlowing)
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
        Vector2 targetSize = new Vector2(1f, fluidElement.height);
        transform.localScale = transform.localScale * targetSize / currentSize;

        // Set transform position
        float midLevel = (fluidElement.upperLevel + fluidElement.lowerLevel) / 2;
        float gridX = fluidElement.position.x;
        float gridY = fluidElement.position.y - 0.5f + midLevel;
        transform.position = MapBoundaryData.MapToWorld(new Vector2(gridX, gridY));
    }
}
