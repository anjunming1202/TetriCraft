using UnityEngine;

/// <summary>
/// Extends <see cref="BlockRenderer"/> with a crack overlay sprite that becomes visible
/// when the bedrock block enters its cracked state (survived one failed destroy attempt).
///
/// Setup:
///   1. Replace the BlockRenderer component on the Bedrock prefab with this component.
///   2. Assign <see cref="crackSprite"/> in the Inspector
///      (e.g. destroy_stage_4.png from the Minecraft Java Edition resource pack).
///   3. Assign <see cref="crackShader"/> → MineTetris/BlockCrackOverlay for visible
///      crack highlights on dark textures. Leave null to fall back to default blend.
/// </summary>
[RequireComponent(typeof(BedrockBlock))]
public class BedrockBlockRenderer : BlockRenderer
{
    [Tooltip("Crack overlay sprite shown when the block has survived one destroy attempt.")]
    [SerializeField] private Sprite crackSprite;

    [Tooltip("Assign MineTetris/BlockCrackOverlay shader for the crack overlay effect.")]
    [SerializeField] private Shader crackShader;

    [Tooltip("Overall opacity of the crack overlay (0 = invisible, 1 = full).")]
    [SerializeField, Range(0f, 1f)] private float crackOpacity = 1f;

    [Tooltip("Brightens crack lines without affecting transparency (1 = original texture brightness, 10 = near white).")]
    [SerializeField, Range(1f, 10f)] private float crackBrightnessBoost = 1f;

    private SpriteRenderer _overlay;
    private BedrockBlock _bedrockBlock;

    protected override void Awake()
    {
        base.Awake();
        _bedrockBlock = (BedrockBlock)block;

        // Create a child SpriteRenderer that sits one order above the base sprite.
        var overlayGO = new GameObject("CrackOverlay");
        overlayGO.transform.SetParent(transform, false);

        _overlay = overlayGO.AddComponent<SpriteRenderer>();
        _overlay.sprite = crackSprite;
        _overlay.sortingLayerID = spriteRenderer.sortingLayerID;
        _overlay.sortingOrder = spriteRenderer.sortingOrder + 1;
        _overlay.maskInteraction = spriteRenderer.maskInteraction;
        _overlay.enabled = false;

        if (crackShader != null)
        {
            var mat = new Material(crackShader);
            mat.SetFloat("_CrackAlpha", crackOpacity);
            mat.SetFloat("_BrightnessBoost", crackBrightnessBoost);
            _overlay.material = mat;
        }
    }

    protected override void Render(Block block)
    {
        base.Render(block);
        if (_overlay != null)
            _overlay.enabled = _bedrockBlock.IsCracked;
    }
}
