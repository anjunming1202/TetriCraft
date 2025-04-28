using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidRenderer : MonoBehaviour
{
    [SerializeField] private Sprite stillTexture;

    private StillFluidBlock fluidBlock;
    private SpriteRenderer spriteRenderer;

    private Material material;
    private MaterialPropertyBlock props;

    void Awake()
    {
        fluidBlock = GetComponent<StillFluidBlock>();
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
        spriteRenderer.sprite = stillTexture;

        // Set material
        spriteRenderer.GetPropertyBlock(props);
        props.SetColor("_Color", spriteRenderer.color); //
        props.SetFloat("_UpperLevel", 1f);
        props.SetFloat("_LowerLevel", 0f);
        spriteRenderer.SetPropertyBlock(props);

        // Set transform size
        Vector2 currentSize = spriteRenderer.bounds.size;
        Vector2 targetSize = new Vector2(1f, 1f);
        transform.localScale = transform.localScale * targetSize / currentSize;
    }
}
