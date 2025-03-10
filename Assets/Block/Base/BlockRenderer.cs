using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

// Manage the rendering of the instantiated block
public class BlockRenderer : MonoBehaviour
{
    // Block Texture
    public Sprite texture;
    public Sprite flashTexture;

    // Renderer
    private SpriteRenderer spriteRenderer;

    // Material
    public Material material;

    // Effect
    private float flashDuration = 0.2f;
    private Coroutine flashCoroutine;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void Initialise(Block block)
    {
        
    }


    /// <summary>
    /// Render block: set position, set texture
    /// </summary>
    public void Render(Block block)
    {
        spriteRenderer.sprite = texture;
    }

    public void FlashOnSet(Block block)
    {
        if (flashCoroutine != null) 
            StopCoroutine(flashCoroutine);
        StartCoroutine(Flash());
    }
    private IEnumerator Flash()
    {
        spriteRenderer.sprite = flashTexture;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.sprite = texture;
    }
}
