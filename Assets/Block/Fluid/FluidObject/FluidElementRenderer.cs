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

    private bool isAnimating;

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

    }

    private void Update()
    {
        Render();
    }

    /// <summary>
    /// Render fluid element: set position, set texture
    /// </summary>
    public void Render()
    {
        if (fluidElement.amount == 0)
        {
            spriteRenderer.enabled = false;
            //return;
        }
        else spriteRenderer.enabled = true;

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

        // Set animation
        if (fluidElement.isFalling || fluidElement.hasFlown)
            isAnimating = true;
        /*else
            isAnimating = false;*/

        // Set material
        spriteRenderer.GetPropertyBlock(props);
        props.SetColor("_Color", spriteRenderer.color); //
        props.SetFloat("_UpperLevel", (float)fluidElement.upperLevel / FluidElement.BlockAmount);
        props.SetFloat("_LowerLevel", (float)fluidElement.lowerLevel / FluidElement.BlockAmount);
        props.SetFloat("_Animation", isAnimating ? 1 : 0);
        spriteRenderer.SetPropertyBlock(props);

        // Set transform size
        if (fluidElement.height > 0)
        {
            Vector2 currentSize = spriteRenderer.bounds.size;
            Vector2 targetSize = new Vector2(fluidElement.width, fluidElement.height);
            transform.localScale = transform.localScale * targetSize / currentSize;
        }

        // Set transform position
        transform.position = MapBoundaryData.MapToWorld(fluidElement.mapPosition);
    }
}
