using UnityEngine;

/// <summary>
/// Extends <see cref="BlockParticleManager"/> with a second particle effect that plays
/// when a bedrock block survives a destroy attempt and transitions to its cracked state.
///
/// Setup:
///   - Replace the <see cref="BlockParticleManager"/> component on the Bedrock prefab
///     with this component.
///   - Assign <c>breakingParticles</c> (inherited) for the normal destroy burst.
///   - Assign <c>crackedParticles</c> for the crack-transition effect.
/// </summary>
[RequireComponent(typeof(BedrockBlock))]
public class BedrockBlockParticleManager : BlockParticleManager
{
    [SerializeField] private ParticleSystem crackedParticles;

    private BedrockBlock _bedrockBlock;
    private SpriteRenderer _spriteRenderer;

    // Cached before any crack transition so HandleBecameCracked always gets the
    // pre-crack sprite, even though the renderer updates synchronously before
    // OnBecameCracked fires (OnTriggerAppearanceChanged runs first).
    private Sprite _preCrackSprite;

    protected override void Awake()
    {
        base.Awake();
        _bedrockBlock    = (BedrockBlock)block;
        _spriteRenderer  = GetComponent<SpriteRenderer>();
        _preCrackSprite  = _spriteRenderer.sprite;
        _bedrockBlock.OnBecameCracked += HandleBecameCracked;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _bedrockBlock.OnBecameCracked -= HandleBecameCracked;
    }

    private void HandleBecameCracked(BedrockBlock b)
    {
        if (crackedParticles == null) return;
        ParticleSystem instance = block.GetMap().SpawnParticle(crackedParticles, transform.position);

        // Apply the pre-crack sprite texture so the particle matches the intact block.
        if (instance != null && _preCrackSprite != null)
        {
            var rend = instance.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                Material mat = new Material(rend.material);
                mat.mainTexture = _preCrackSprite.texture;
                rend.material = mat;
            }
        }
    }
}
